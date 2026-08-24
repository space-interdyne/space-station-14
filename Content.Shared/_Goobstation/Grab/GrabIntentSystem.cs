// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Goobstation.Grab;

public sealed partial class GrabIntentSystem : EntitySystem
{
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedVirtualItemSystem _virtualSystem = default!;
    [Dependency] private AlertsSystem _alertsSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private MovementSpeedModifierSystem _modifierSystem = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private HeldSpeedModifierSystem _clothingMoveSpeed = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private GrabThrownSystem _grabThrown = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private INetManager _net = default!;

    private readonly SoundPathSpecifier _thudswoosh = new("/Audio/Effects/thudswoosh.ogg");

    public override void Initialize()
    {
        InitializeCoreEvents();
        InitializeGrabStageEvents();
        InitializeReleaseAndThrowEvents();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<GrabbableComponent, PullableComponent>();
        while (query.MoveNext(out var uid, out var grabbable, out var pullable))
        {
            if (grabbable.GrabStage != GrabStage.Suffocate)
                continue;

            if (pullable.Puller is not { } puller
                || !TryComp<GrabIntentComponent>(puller, out var grabIntent)
                || grabIntent.GrabStage != GrabStage.Suffocate)
                continue;

            if (_timing.CurTime < grabbable.NextSuffocateDamage)
                continue;

            grabbable.NextSuffocateDamage = _timing.CurTime + grabIntent.SuffocateGrabDamageInterval;
            Dirty(uid, grabbable);

            // Base asphyxiation comes from RespiratorSystem.CanBreathe (gasp + vacuum damage).
            // Only extra sources like a garrote apply bonus here.
            var damageEv = new ModifySuffocateGrabDamageEvent(0f);
            RaiseLocalEvent(puller, ref damageEv);
            RaiseLocalEvent(uid, ref damageEv);

            if (damageEv.Bonus <= 0)
                continue;

            _damageable.TryChangeDamage(uid, new DamageSpecifier
            {
                DamageDict = { ["Asphyxiation"] = damageEv.Bonus }
            });
        }
    }

    private void InitializeCoreEvents()
    {
        SubscribeLocalEvent<GrabbableComponent, MoveInputEvent>(OnPullableMoveInput);
        SubscribeLocalEvent<GrabbableComponent, CheckGrabbedEvent>(OnCheckGrabbed);
        SubscribeLocalEvent<GrabbableComponent, GrabAttemptEvent>(OnGrabAttempt);
        SubscribeLocalEvent<GrabbableComponent, PullStoppedMessage>(OnPullStoppedGrabbable);
        SubscribeLocalEvent<GrabIntentComponent, PullStoppedMessage>(OnPullStoppedGrabIntent);
    }

    private void OnPullStoppedGrabbable(EntityUid uid, GrabbableComponent component, ref PullStoppedMessage args)
    {
        if (args.PulledUid != uid)
            return;

        component.GrabStage = GrabStage.No;
        component.GrabEscapeChance = 1f;
        component.EscapeAttemptModifier = 1f;
        component.NextSuffocateDamage = TimeSpan.Zero;
        _blocker.UpdateCanMove(uid);
        _alertsSystem.ClearAlert(uid, component.PulledAlert);
        Dirty(uid, component);
    }

    private void OnPullStoppedGrabIntent(EntityUid uid, GrabIntentComponent component, ref PullStoppedMessage args)
    {
        if (args.PullerUid != uid)
            return;

        component.GrabStage = GrabStage.No;

        foreach (var item in GetGrabVirtualItems(uid, args.PulledUid).ToList())
        {
            if (TryComp<VirtualItemComponent>(item, out var vi))
                _virtualSystem.DeleteVirtualItem((item, vi), uid);
            else
                QueueDel(item);
        }

        Dirty(uid, component);
    }

    private void OnCheckGrabbed(EntityUid uid, GrabbableComponent component, ref CheckGrabbedEvent args)
    {
        args.IsGrabbed = component.GrabStage != GrabStage.No;
    }

    private void OnGrabAttempt(Entity<GrabbableComponent> ent, ref GrabAttemptEvent args)
    {
        if (!TryComp<PullableComponent>(ent, out var pullable))
            return;

        args.Grabbed = TryGrab((ent.Owner, pullable, ent.Comp),
            args.Puller,
            args.IgnoreCombatMode,
            args.GrabStageOverride,
            args.EscapeAttemptModifier);
    }

    private void OnPullableMoveInput(EntityUid uid, GrabbableComponent component, ref MoveInputEvent args)
    {
        if (!TryComp<PullableComponent>(uid, out var pullable) || !pullable.BeingPulled)
            return;

        if (component.GrabStage == GrabStage.Soft && _blocker.CanInteract(uid, null))
            _pulling.TryStopPull(uid, pullable, uid);

        if (!_blocker.CanMove(args.Entity))
            return;

        _pulling.TryStopPull(uid, pullable, user: uid);
    }

    public bool CanGrab(EntityUid puller, EntityUid pullable)
    {
        return !HasComp<PacifiedComponent>(puller) && HasComp<MobStateComponent>(pullable);
    }

    public void ThrowGrabbedEntity(Entity<PullerComponent?, GrabIntentComponent?, PhysicsComponent?> ent, Vector2 dir)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2, ref ent.Comp3, false)
            || ent.Comp1.Pulling is not { } pulling
            || !TryComp(pulling, out PullableComponent? pullingPullableComp))
            return;

        if (!_combatMode.IsInCombatMode(ent.Owner)
            || HasComp<GrabThrownComponent>(pulling)
            || ent.Comp2.GrabStage <= GrabStage.Soft)
            return;

        var distanceToCursor = dir.Length();
        var direction = dir.Normalized() * MathF.Min(distanceToCursor, ent.Comp2.ThrowingDistance);

        var damage = new DamageSpecifier();
        damage.DamageDict.Add(ent.Comp2.GrabThrowDamageType, ent.Comp2.GrabThrowDamage);

        _pulling.TryStopPull(pulling, pullingPullableComp, ent.Owner, true);
        _grabThrown.Throw(pulling,
            ent.Owner,
            direction,
            ent.Comp2.GrabThrownSpeed,
            damage * ent.Comp2.GrabThrowDamageModifier);
        _throwing.TryThrow(ent.Owner, -direction * ent.Comp3.InvMass);
        _audio.PlayPredicted(ent.Comp2.GrabSoundEffect, pulling, ent.Owner);
        ent.Comp2.NextStageChange = _timing.CurTime.Add(TimeSpan.FromSeconds(3f));
        Dirty(ent.Owner, ent.Comp2);
    }
}
