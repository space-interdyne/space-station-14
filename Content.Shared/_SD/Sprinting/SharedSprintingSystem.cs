using System.Numerics;
using Content.Shared._SD.Input;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Zombies;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._SD.Sprinting;

public abstract partial class SharedSprintingSystem : EntitySystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedMoverController _moverController = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SprinterComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        CommandBinds.Builder
            .Bind(SDKeyFunctions.Sprint, new SprintInputCmdHandler(this))
            .Register<SharedSprintingSystem>();
        SubscribeLocalEvent<SprinterComponent, SprintToggleEvent>(OnSprintToggle);
        SubscribeLocalEvent<SprinterComponent, MobStateChangedEvent>(OnMobStateChangedEvent);
        SubscribeLocalEvent<SprinterComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<SprinterComponent, SleepStateChangedEvent>(OnSleep);
        SubscribeLocalEvent<SprinterComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<SprinterComponent, KnockedDownEvent>(OnSprintDisablingEvent);
        SubscribeLocalEvent<SprinterComponent, StunnedEvent>(OnSprintDisablingEvent);
        SubscribeLocalEvent<SprinterComponent, DownedEvent>(OnSprintDisablingEvent);
        SubscribeLocalEvent<CuffableComponent, SprintAttemptEvent>(OnCuffableSprintAttempt);
        SubscribeLocalEvent<StandingStateComponent, SprintAttemptEvent>(OnStandingStateSprintAttempt);
        SubscribeLocalEvent<BuckleComponent, SprintAttemptEvent>(OnBuckleSprintAttempt);
        SubscribeLocalEvent<SprinterComponent, EntityZombifiedEvent>(OnZombified);
        SubscribeLocalEvent<SprinterComponent, DisarmedEvent>(OnDisarm);
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<SharedSprintingSystem>();
    }

    #region Core Functions

    private sealed class SprintInputCmdHandler(SharedSprintingSystem system) : InputCmdHandler
    {
        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            if (session?.AttachedEntity == null)
                return false;

            system.HandleSprintInput(session, message);
            return false;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SprinterComponent, StaminaComponent>();
        while (query.MoveNext(out var uid, out var sprinter, out var stamina))
        {
            if (!sprinter.IsSprinting)
                continue;

            var drain = sprinter.StaminaDrainRate;
            if (sprinter.ScaleWithStamina
                && TryComp<StaminaModifierStatusEffectComponent>(uid, out var staminaMod)
                && staminaMod.Modifier > 1f)
            {
                drain *= staminaMod.Modifier * sprinter.StaminaDrainMultiplier;
            }

            _stamina.TakeStaminaDamage(uid, drain * frameTime, stamina, visual: false);

            if (stamina.Critical || stamina.StaminaDamage >= stamina.CritThreshold)
                ToggleSprint(uid, sprinter, false, gracefulStop: false);
        }
    }

    private void OnRefreshSpeed(Entity<SprinterComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.IsSprinting)
            return;

        args.ModifySpeed(ent.Comp.SprintSpeedMultiplier);
    }

    private void HandleSprintInput(ICommonSession? session, IFullInputCmdMessage message)
    {
        if (session?.AttachedEntity == null
            || !TryComp<SprinterComponent>(session.AttachedEntity, out var sprinterComponent)
            || !TryComp<InputMoverComponent>(session.AttachedEntity, out var inputMoverComponent)
            || !sprinterComponent.IsSprinting
            // Gatekeep sprinting to intentional movement, not standing still.
            && _moverController.GetVelocityInput(inputMoverComponent).Sprinting == Vector2.Zero)
            return;

        if (!sprinterComponent.CanSprint)
        {
            if (message.State == BoundKeyState.Down)
                _popup.PopupEntity(Loc.GetString("sprint-disabled"), session.AttachedEntity.Value, session.AttachedEntity.Value, PopupType.Medium);

            return;
        }

        RaiseLocalEvent(session.AttachedEntity.Value, new SprintToggleEvent(!sprinterComponent.IsSprinting && message.State == BoundKeyState.Down));
    }

    private void OnSprintToggle(EntityUid uid, SprinterComponent component, ref SprintToggleEvent args) =>
        ToggleSprint(uid, component, args.IsSprinting);

    public void ToggleSprint(EntityUid uid, SprinterComponent component, bool newSprintState, bool gracefulStop = true)
    {
        if (newSprintState == component.IsSprinting)
            return;

        if (newSprintState
            && (!CanSprint(uid, component)
            || _timing.CurTime - component.LastSprint < component.TimeBetweenSprints))
            return;

        component.LastSprint = _timing.CurTime;
        component.IsSprinting = newSprintState;

        if (newSprintState)
        {
            RaiseLocalEvent(uid, new SprintStartEvent());
            _audio.PlayPredicted(component.SprintStartupSound, uid, uid);
        }

        if (!gracefulStop)
            _damageable.TryChangeDamage(uid, component.SprintDamageSpecifier);

        _movementSpeed.RefreshMovementSpeedModifiers(uid);
        Dirty(uid, component);
    }

    #endregion

    #region Conditionals

    private bool CanSprint(EntityUid uid, SprinterComponent component)
    {
        if (_gravity.IsWeightless(uid))
        {
            _popup.PopupEntity(Loc.GetString("no-sprint-while-weightless"), uid, uid, PopupType.Medium);
            return false;
        }

        var ev = new SprintAttemptEvent();
        RaiseLocalEvent(uid, ref ev);

        return !ev.Cancelled;
    }

    private void OnCuffableSprintAttempt(EntityUid uid, CuffableComponent component, ref SprintAttemptEvent args)
    {
        if (component.CanStillInteract)
            return;

        _popup.PopupEntity(Loc.GetString("no-sprint-while-restrained"), uid, uid, PopupType.Medium);
        args.Cancel();
    }

    private void OnStandingStateSprintAttempt(EntityUid uid, StandingStateComponent component, ref SprintAttemptEvent args)
    {
        if (!_standing.IsDown(uid))
            return;

        _popup.PopupEntity(Loc.GetString("no-sprint-while-lying"), uid, uid, PopupType.Medium);
        args.Cancel();
    }

    private void OnBuckleSprintAttempt(EntityUid uid, BuckleComponent component, ref SprintAttemptEvent args)
    {
        if (component.BuckledTo == null
            || !TryComp<SprinterComponent>(component.BuckledTo, out var sprinterComponent)
            || sprinterComponent.IsSprinting)
            return;

        args.Cancel();
    }

    #endregion

    #region Misc.Handlers

    private void OnBeforeStaminaDamage(EntityUid uid, SprinterComponent component, ref BeforeStaminaDamageEvent args)
    {
        if (!component.IsSprinting
            || args.Value > 0)
            return;

        args.Value *= component.StaminaRegenMultiplier;
    }

    private void OnMobStateChangedEvent(EntityUid uid, SprinterComponent component, MobStateChangedEvent args)
    {
        if (!component.IsSprinting
            || args.NewMobState is MobState.Critical or MobState.Dead)
            return;

        ToggleSprint(args.Target, component, false, gracefulStop: false);
    }

    private void OnSleep(EntityUid uid, SprinterComponent component, ref SleepStateChangedEvent args)
    {
        if (!component.IsSprinting
            || !args.FellAsleep)
            return;

        ToggleSprint(uid, component, false, gracefulStop: false);
    }

    private void OnMoveInput(EntityUid uid, SprinterComponent component, ref MoveInputEvent args)
    {
        if (!component.IsSprinting)
            return;

        // Stop sprint when the player starts walking (Shift).
        var wasWalking = (args.OldMovement & MoveButtons.Walk) != 0;
        var isWalking = (args.Entity.Comp.HeldMoveButtons & MoveButtons.Walk) != 0;
        if (!wasWalking && isWalking)
            ToggleSprint(uid, component, false);
    }

    private void OnSprintDisablingEvent<T>(EntityUid uid, SprinterComponent component, ref T args) where T : notnull
    {
        if (!component.IsSprinting)
            return;

        ToggleSprint(uid, component, false, gracefulStop: false);
    }

    private void OnZombified(EntityUid uid, SprinterComponent component, ref EntityZombifiedEvent args) =>
        component.SprintSpeedMultiplier *= 0.5f;

    private void OnDisarm(EntityUid uid, SprinterComponent sprinter, ref DisarmedEvent args)
    {
        if (!sprinter.IsSprinting)
            return;

        _stamina.TakeStaminaDamage(uid, sprinter.StaminaPenaltyOnShove, visual: false);
        ToggleSprint(uid, sprinter, false);
    }

    #endregion
}
