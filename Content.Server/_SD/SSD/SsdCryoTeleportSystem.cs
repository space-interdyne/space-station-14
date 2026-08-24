using System.Diagnostics.CodeAnalysis;
using Content.Server.GameTicking;
using Content.Shared._SD.CCVar;
using Content.Shared.Administration.Logs;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Buckle;
using Content.Shared.Database;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.SSDIndicator;
using Content.Shared.Station;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._SD.SSD;

public sealed class SsdCryoTeleportSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private LinkedEntitySystem _link = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedCryoPodSystem _cryoPod = default!;
    [Dependency] private SharedStationSystem _station = default!;

    private static readonly EntProtoId PortalPrototype = "PortalSsdCryo";

    private bool _enabled;
    private float _ssdTime;
    private float _portalDelay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SSDIndicatorComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<SSDIndicatorComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SsdCryoTeleportComponent, ComponentShutdown>(OnShutdown);

        _cfg.OnValueChanged(SDCCVars.SsdCryoTeleportEnabled, v => _enabled = v, true);
        _cfg.OnValueChanged(SDCCVars.SsdCryoTeleportTime, v => _ssdTime = v, true);
        _cfg.OnValueChanged(SDCCVars.SsdCryoPortalDelay, v => _portalDelay = v, true);
    }

    private void OnPlayerDetached(EntityUid uid, SSDIndicatorComponent component, PlayerDetachedEvent args)
    {
        if (!_enabled || TerminatingOrDeleted(uid) || !CanTeleport(uid))
            return;

        var comp = EnsureComp<SsdCryoTeleportComponent>(uid);
        comp.PortalSpawnTime = _timing.CurTime + TimeSpan.FromSeconds(_ssdTime);
        comp.TeleportTime = null;
        DeletePortals(comp);
    }

    private void OnPlayerAttached(EntityUid uid, SSDIndicatorComponent component, PlayerAttachedEvent args)
    {
        RemCompDeferred<SsdCryoTeleportComponent>(uid);
    }

    private void OnShutdown(Entity<SsdCryoTeleportComponent> ent, ref ComponentShutdown args)
    {
        DeletePortals(ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled || _ticker.RunLevel != GameRunLevel.InRound)
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SsdCryoTeleportComponent, SSDIndicatorComponent, TransformComponent, MetaDataComponent>();

        while (query.MoveNext(out var uid, out var teleport, out var ssd, out _, out var meta))
        {
            if (meta.EntityPaused || TerminatingOrDeleted(uid))
                continue;

            if (!ssd.IsSSD || !CanTeleport(uid))
            {
                RemCompDeferred<SsdCryoTeleportComponent>(uid);
                continue;
            }

            if (teleport.SourcePortal is { } source && TerminatingOrDeleted(source))
                DeletePortals(teleport);

            if (teleport.SourcePortal == null)
            {
                if (curTime < teleport.PortalSpawnTime)
                    continue;

                if (!TrySpawnPortals(uid, teleport))
                    continue;
            }
            else if (teleport.TeleportTime != null && curTime >= teleport.TeleportTime)
            {
                if (TryTeleportToCryo(uid, teleport))
                    RemCompDeferred<SsdCryoTeleportComponent>(uid);
            }
        }
    }

    private bool CanTeleport(EntityUid uid)
    {
        if (!HasComp<MobStateComponent>(uid))
            return false;

        if (HasComp<InsideCryoPodComponent>(uid) || HasComp<CryostorageContainedComponent>(uid))
            return false;

        return true;
    }

    private bool TrySpawnPortals(EntityUid body, SsdCryoTeleportComponent teleport)
    {
        if (!TryFindEmptyCryoPod(body, out var cryo))
            return false;

        PrepareBody(body);

        if (!TrySpawnNextTo(PortalPrototype, body, out var source) ||
            !TrySpawnNextTo(PortalPrototype, cryo.Value.Owner, out var destination))
        {
            if (source != null)
                QueueDel(source.Value);
            if (destination != null)
                QueueDel(destination.Value);
            return false;
        }

        _link.TryLink(source.Value, destination.Value, true);

        teleport.SourcePortal = source;
        teleport.DestinationPortal = destination;
        teleport.TeleportTime = _timing.CurTime + TimeSpan.FromSeconds(Math.Max(_portalDelay, 0.1f));
        return true;
    }

    private bool TryTeleportToCryo(EntityUid body, SsdCryoTeleportComponent teleport)
    {
        if (!TryFindEmptyCryoPod(body, out var cryo))
        {
            ScheduleRetry(teleport);
            return false;
        }

        PrepareBody(body);

        if (!_cryoPod.InsertBody(cryo.Value.Owner, body, cryo.Value.Comp))
        {
            ScheduleRetry(teleport);
            return false;
        }

        _adminLog.Add(LogType.Teleport,
            LogImpact.Medium,
            $"{ToPrettyString(body):player} was moved into cryo pod {ToPrettyString(cryo.Value.Owner)} after SSD timeout");

        DeletePortals(teleport);
        return true;
    }

    private void ScheduleRetry(SsdCryoTeleportComponent teleport)
    {
        DeletePortals(teleport);
        teleport.TeleportTime = null;
        teleport.PortalSpawnTime = _timing.CurTime + TimeSpan.FromSeconds(30);
    }

    private void PrepareBody(EntityUid body)
    {
        if (TryComp<PullableComponent>(body, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(body, pullable, ignoreGrab: true);

        if (TryComp<PullerComponent>(body, out var puller) &&
            puller.Pulling is { } pulled &&
            TryComp<PullableComponent>(pulled, out var pulledComp))
        {
            _pulling.TryStopPull(pulled, pulledComp, ignoreGrab: true);
        }

        _buckle.Unbuckle(body, user: null);

        while (_container.IsEntityInContainer(body) && _container.TryRemoveFromContainer(body, force: true))
        {
        }
    }

    private bool TryFindEmptyCryoPod(EntityUid body, [NotNullWhen(true)] out Entity<CryoPodComponent>? cryo)
    {
        cryo = null;

        var station = _station.GetOwningStation(body);
        var mapId = Transform(body).MapID;

        var stationCandidates = new List<Entity<CryoPodComponent>>();
        var mapCandidates = new List<Entity<CryoPodComponent>>();

        var query = EntityQueryEnumerator<CryoPodComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (comp.BodyContainer?.ContainedEntity != null)
                continue;

            if (xform.MapID == MapId.Nullspace)
                continue;

            if (xform.MapID == mapId)
                mapCandidates.Add((uid, comp));

            if (station != null && _station.GetOwningStation(uid, xform) == station)
                stationCandidates.Add((uid, comp));
        }

        var candidates = stationCandidates.Count > 0 ? stationCandidates : mapCandidates;
        if (candidates.Count == 0)
            return false;

        cryo = _random.Pick(candidates);
        return true;
    }

    private void DeletePortals(SsdCryoTeleportComponent teleport)
    {
        if (teleport.SourcePortal is { } source && !TerminatingOrDeleted(source))
            QueueDel(source);

        if (teleport.DestinationPortal is { } dest && !TerminatingOrDeleted(dest))
            QueueDel(dest);

        teleport.SourcePortal = null;
        teleport.DestinationPortal = null;
    }
}
