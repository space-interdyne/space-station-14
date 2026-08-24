using System.Numerics;
using Content.Shared.Charges.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Gibbing;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._SD.Teleportation;

public sealed partial class SyndicateTeleporterSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private static readonly SoundCollectionSpecifier TeleportSound = new("SDSparks");
    private static readonly SoundSpecifier FailSound =
        new SoundPathSpecifier("/Audio/_SD/Effects/disintegrate.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyndicateTeleporterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SyndicateTeleporterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<SyndicateTeleporterComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnMapInit(Entity<SyndicateTeleporterComponent> ent, ref MapInitEvent args)
    {
        UpdateChargeVisuals(ent.Owner);
    }

    private void OnUseInHand(Entity<SyndicateTeleporterComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        AttemptTeleport(ent, args.User, unsafeMode: false);
    }

    private void OnEmpPulse(Entity<SyndicateTeleporterComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        if (Transform(ent).ParentUid is { Valid: true } parent && parent != ent.Owner)
            AttemptTeleport(ent, parent, unsafeMode: true);
    }

    private void AttemptTeleport(Entity<SyndicateTeleporterComponent> ent, EntityUid user, bool unsafeMode)
    {
        if (!unsafeMode && !_charges.HasCharges((ent.Owner, null), 1))
        {
            _popup.PopupClient(Loc.GetString("syndicate-teleporter-empty"), user, user, PopupType.SmallCaution);
            return;
        }

        if (TryComp(user, out PullerComponent? puller) && puller.Pulling is { } pulled
            && TryComp(pulled, out PullableComponent? pulledComp))
        {
            _pulling.TryStopPull(pulled, pulledComp, user);
        }

        if (TryComp(user, out PullableComponent? pullable) && pullable.Puller != null)
            _pulling.TryStopPull(user, pullable);

        if (_net.IsClient)
            return;

        var xform = Transform(user);
        var origin = _transform.GetMapCoordinates(user, xform);
        var facing = _transform.GetWorldRotation(xform).GetCardinalDir().ToVec();

        var candidates = new List<EntityCoordinates>();
        for (var dist = ent.Comp.MinRange + 1f; dist <= ent.Comp.MaxRange; dist += 1f)
        {
            var mapPos = origin.Offset(facing * dist);
            var coords = _transform.ToCoordinates(mapPos);
            if (IsSafeLanding(user, coords))
                candidates.Add(coords);
        }

        if (candidates.Count == 0)
        {
            var ideal = _transform.ToCoordinates(origin.Offset(facing * ((ent.Comp.MinRange + ent.Comp.MaxRange) / 2f)));
            if (!unsafeMode && TrySavingThrow(user, ideal, facing, ent.Comp.SavingThrowDistance, out var safe))
            {
                DoTeleport(user, safe);
                TryConsumeCharge(ent.Owner, unsafeMode);
                return;
            }

            FailTeleport(user, ideal);
            TryConsumeCharge(ent.Owner, unsafeMode);
            return;
        }

        var destination = _random.Pick(candidates);
        DoTeleport(user, destination);

        foreach (var other in _turf.GetEntitiesInTile(destination, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (other == user)
                continue;

            _damageable.TryChangeDamage(other, new DamageSpecifier
            {
                DamageDict = { ["Blunt"] = 20 }
            });
            _stun.TryUpdateParalyzeDuration(other, TimeSpan.FromSeconds(3));
        }

        TryConsumeCharge(ent.Owner, unsafeMode);
    }

    private void TryConsumeCharge(EntityUid teleporter, bool unsafeMode)
    {
        if (!unsafeMode)
            _charges.TryUseCharge((teleporter, null));

        UpdateChargeVisuals(teleporter);
    }

    private void UpdateChargeVisuals(EntityUid uid)
    {
        var charges = _charges.GetCurrentCharges((uid, null));
        _appearance.SetData(uid, SyndicateTeleporterVisuals.Charges, charges);
    }

    private bool TrySavingThrow(EntityUid user, EntityCoordinates unsafeDest, Vector2 facing, float distance, out EntityCoordinates safe)
    {
        safe = default;
        var perpendiculars = new[]
        {
            new Vector2(-facing.Y, facing.X),
            new Vector2(facing.Y, -facing.X),
        };

        var options = new List<EntityCoordinates>();
        foreach (var perp in perpendiculars)
        {
            for (var d = 1f; d <= distance; d += 1f)
            {
                var map = _transform.ToMapCoordinates(unsafeDest).Offset(perp * d);
                var coords = _transform.ToCoordinates(map);
                if (IsSafeLanding(user, coords))
                    options.Add(coords);
            }
        }

        if (options.Count == 0)
            return false;

        safe = _random.Pick(options);
        return true;
    }

    private void DoTeleport(EntityUid user, EntityCoordinates destination)
    {
        _audio.PlayPvs(TeleportSound, Transform(user).Coordinates);
        _transform.SetCoordinates(user, destination);
        _audio.PlayPvs(TeleportSound, destination);
        _popup.PopupEntity(Loc.GetString("syndicate-teleporter-success"), user, user, PopupType.Small);
    }

    private void FailTeleport(EntityUid user, EntityCoordinates destination)
    {
        _audio.PlayPvs(FailSound, destination);
        _transform.SetCoordinates(user, destination);
        _popup.PopupEntity(Loc.GetString("syndicate-teleporter-wall-fail"), user, user, PopupType.LargeCaution);
        _gibbing.Gib(user);
    }

    private bool IsSafeLanding(EntityUid user, EntityCoordinates coords)
    {
        if (!_turf.TryGetTileRef(coords, out var tileRef))
            return false;

        if (_turf.IsSpace(tileRef.Value))
            return true;

        var mask = CollisionGroup.MobMask;
        if (TryComp(user, out PhysicsComponent? physics))
            mask = (CollisionGroup) physics.CollisionMask;

        return !_turf.IsTileBlocked(tileRef.Value, mask);
    }
}
