using System.Diagnostics.CodeAnalysis;
using System.Threading;
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
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._SD.SSD;

public sealed partial class SsdCryoTeleportSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private LinkedEntitySystem _link = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedStationSystem _station = default!;

    private static readonly EntProtoId PortalPrototype = "PortalSsdCryo";
    private static readonly SoundSpecifier DefaultDepartureSound =
        new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
    private static readonly SoundSpecifier DefaultArrivalSound =
        new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    private bool _enabled;
    private float _ssdTime;
    private float _portalDelay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SsdCryoTeleportComponent, ComponentShutdown>(OnShutdown);

        _cfg.OnValueChanged(SDCCVars.SsdCryoTeleportEnabled, v => _enabled = v, true);
        _cfg.OnValueChanged(SDCCVars.SsdCryoTeleportTime, v => _ssdTime = v, true);
        _cfg.OnValueChanged(SDCCVars.SsdCryoPortalDelay, v => _portalDelay = v, true);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        var uid = args.Entity;
        if (!_enabled || !HasComp<SSDIndicatorComponent>(uid) || TerminatingOrDeleted(uid) || !CanTeleport(uid))
            return;

        var comp = EnsureComp<SsdCryoTeleportComponent>(uid);
        DeletePortals(comp);
        comp.TargetCryostorage = null;
        SchedulePortalSpawn(uid, comp, TimeSpan.FromSeconds(_ssdTime));
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        RemCompDeferred<SsdCryoTeleportComponent>(args.Entity);
    }

    private void OnShutdown(Entity<SsdCryoTeleportComponent> ent, ref ComponentShutdown args)
    {
        CancelTimers(ent.Comp);
        DeletePortals(ent.Comp);
    }

    private void SchedulePortalSpawn(EntityUid uid, SsdCryoTeleportComponent comp, TimeSpan delay)
    {
        CancelTimers(comp);
        comp.TimerCancel = new CancellationTokenSource();
        Timer.Spawn(delay, () => OnPortalSpawnTimer(uid), comp.TimerCancel.Token);
    }

    private void ScheduleTeleport(EntityUid uid, SsdCryoTeleportComponent comp)
    {
        CancelTimers(comp);
        comp.TimerCancel = new CancellationTokenSource();
        Timer.Spawn(TimeSpan.FromSeconds(Math.Max(_portalDelay, 0.1f)), () => OnTeleportTimer(uid), comp.TimerCancel.Token);
    }

    private void OnPortalSpawnTimer(EntityUid uid)
    {
        if (!_enabled ||
            _ticker.RunLevel != GameRunLevel.InRound ||
            TerminatingOrDeleted(uid) ||
            !TryComp<SsdCryoTeleportComponent>(uid, out var teleport) ||
            !TryComp<SSDIndicatorComponent>(uid, out var ssd) ||
            !ssd.IsSSD ||
            !CanTeleport(uid))
        {
            RemCompDeferred<SsdCryoTeleportComponent>(uid);
            return;
        }

        if (!TryFindEmptyCryostorage(uid, out var cryo))
        {
            SchedulePortalSpawn(uid, teleport, RetryDelay);
            return;
        }

        PrepareBody(uid);

        EntityUid? source = null;
        EntityUid? destination = null;
        if (!TrySpawnNextTo(PortalPrototype, uid, out source) ||
            !TrySpawnNextTo(PortalPrototype, cryo.Value.Owner, out destination))
        {
            if (source != null)
                QueueDel(source.Value);
            if (destination != null)
                QueueDel(destination.Value);

            SchedulePortalSpawn(uid, teleport, RetryDelay);
            return;
        }

        _link.TryLink(source.Value, destination.Value, true);

        teleport.TargetCryostorage = cryo.Value.Owner;
        teleport.SourcePortal = source;
        teleport.DestinationPortal = destination;
        ScheduleTeleport(uid, teleport);
    }

    private void OnTeleportTimer(EntityUid uid)
    {
        if (!_enabled ||
            _ticker.RunLevel != GameRunLevel.InRound ||
            TerminatingOrDeleted(uid) ||
            !TryComp<SsdCryoTeleportComponent>(uid, out var teleport) ||
            !TryComp<SSDIndicatorComponent>(uid, out var ssd) ||
            !ssd.IsSSD ||
            !CanTeleport(uid))
        {
            RemCompDeferred<SsdCryoTeleportComponent>(uid);
            return;
        }

        if (!TryGetTargetCryostorage(uid, teleport, out var cryo))
        {
            DeletePortals(teleport);
            teleport.TargetCryostorage = null;
            SchedulePortalSpawn(uid, teleport, RetryDelay);
            return;
        }

        PrepareBody(uid);

        if (!_container.TryGetContainer(cryo.Value.Owner, cryo.Value.Comp.ContainerId, out var container) ||
            !_container.Insert(uid, container))
        {
            DeletePortals(teleport);
            teleport.TargetCryostorage = null;
            SchedulePortalSpawn(uid, teleport, RetryDelay);
            return;
        }

        PlayTeleportSounds(teleport, cryo.Value.Owner);

        _adminLog.Add(LogType.Teleport,
            LogImpact.Medium,
            $"{ToPrettyString(uid):player} was moved into cryogenic sleep unit {ToPrettyString(cryo.Value.Owner)} after SSD timeout");

        DeletePortals(teleport);
        RemCompDeferred<SsdCryoTeleportComponent>(uid);
    }

    private bool TryGetTargetCryostorage(
        EntityUid body,
        SsdCryoTeleportComponent teleport,
        [NotNullWhen(true)] out Entity<CryostorageComponent>? cryo)
    {
        cryo = null;

        if (teleport.TargetCryostorage is { } target &&
            !TerminatingOrDeleted(target) &&
            TryComp<CryostorageComponent>(target, out var targetComp) &&
            _container.TryGetContainer(target, targetComp.ContainerId, out var container) &&
            container.ContainedEntities.Count == 0)
        {
            cryo = (target, targetComp);
            return true;
        }

        // Reserved target was lost/filled; fall back to another empty unit.
        return TryFindEmptyCryostorage(body, out cryo);
    }

    private void PlayTeleportSounds(SsdCryoTeleportComponent teleport, EntityUid destination)
    {
        var departure = DefaultDepartureSound;
        var arrival = DefaultArrivalSound;

        if (teleport.SourcePortal is { } source && TryComp<PortalComponent>(source, out var sourcePortal))
            departure = sourcePortal.DepartureSound;

        if (teleport.DestinationPortal is { } destPortal && TryComp<PortalComponent>(destPortal, out var portal))
            arrival = portal.ArrivalSound;

        if (teleport.SourcePortal is { } sourceUid && !TerminatingOrDeleted(sourceUid))
            _audio.PlayPvs(departure, sourceUid);
        else
            _audio.PlayPvs(departure, destination);

        _audio.PlayPvs(arrival, destination);
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

    private bool CanTeleport(EntityUid uid)
    {
        if (!HasComp<MobStateComponent>(uid))
            return false;

        if (HasComp<InsideCryoPodComponent>(uid) || HasComp<CryostorageContainedComponent>(uid))
            return false;

        return true;
    }

    private bool TryFindEmptyCryostorage(EntityUid body, [NotNullWhen(true)] out Entity<CryostorageComponent>? cryo)
    {
        cryo = null;

        var station = _station.GetOwningStation(body);
        var mapId = Transform(body).MapID;

        var stationCandidates = new List<Entity<CryostorageComponent>>();
        var mapCandidates = new List<Entity<CryostorageComponent>>();

        var query = EntityQueryEnumerator<CryostorageComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (IsCryostorageReserved(uid, exceptBody: body))
                continue;

            if (!_container.TryGetContainer(uid, comp.ContainerId, out var container) ||
                container.ContainedEntities.Count > 0)
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

    private bool IsCryostorageReserved(EntityUid cryostorage, EntityUid exceptBody)
    {
        var query = EntityQueryEnumerator<SsdCryoTeleportComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (uid == exceptBody)
                continue;

            if (comp.TargetCryostorage == cryostorage)
                return true;
        }

        return false;
    }

    private void CancelTimers(SsdCryoTeleportComponent teleport)
    {
        teleport.TimerCancel?.Cancel();
        teleport.TimerCancel = null;
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
