using System.Numerics;
using Content.Shared._Goobstation.Grab;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._SD.Garrote;

public sealed partial class GarroteSystem : EntitySystem
{
    [Dependency] private GrabIntentSystem _grabIntent = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private static readonly SoundSpecifier GarroteSound =
        new SoundPathSpecifier("/Audio/_SD/Weapons/cablecuff.ogg");

    private static readonly EntProtoId MutedEffect = "StatusEffectMuted";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GarroteComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<GrabIntentComponent, FindGrabbingItemEvent>(OnFindGrabbingItem);
        SubscribeLocalEvent<GrabIntentComponent, ModifySuffocateGrabDamageEvent>(OnModifySuffocateDamage);
    }

    private void OnModifySuffocateDamage(Entity<GrabIntentComponent> ent, ref ModifySuffocateGrabDamageEvent args)
    {
        if (ent.Comp.GrabStage != GrabStage.Suffocate)
            return;

        foreach (var held in _hands.EnumerateHeld(ent.Owner))
        {
            if (!TryComp<GarroteComponent>(held, out var garrote))
                continue;

            args.Bonus += garrote.BonusAsphyxiationDamage;
            return;
        }
    }

    private void OnFindGrabbingItem(Entity<GrabIntentComponent> ent, ref FindGrabbingItemEvent args)
    {
        if (args.GrabbingItem != null)
            return;

        foreach (var held in _hands.EnumerateHeld(ent.Owner))
        {
            if (!HasComp<GarroteComponent>(held))
                continue;

            args.GrabbingItem = held;
            return;
        }
    }

    private void OnMeleeHit(Entity<GarroteComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (_hands.CountFreeHands(args.User) < 1)
        {
            _popup.PopupClient(Loc.GetString("garrote-must-wield"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        foreach (var target in args.HitEntities)
        {
            if (!_mobState.IsAlive(target))
                continue;

            if (!IsBehind(args.User, target, ent.Comp) && !_mobState.IsIncapacitated(target))
            {
                _popup.PopupClient(Loc.GetString("garrote-must-be-behind"), args.User, args.User, PopupType.SmallCaution);
                continue;
            }

            if (!_pulling.TryStartPull(args.User, target, grabStageOverride: GrabStage.Suffocate))
            {
                _popup.PopupClient(Loc.GetString("garrote-failed"), args.User, args.User, PopupType.SmallCaution);
                continue;
            }

            if (!TryForceSuffocateGrab(args.User, target))
            {
                if (TryComp(target, out PullableComponent? failedPullable))
                    _pulling.TryStopPull(target, failedPullable, ignoreGrab: true);

                _popup.PopupClient(Loc.GetString("garrote-failed"), args.User, args.User, PopupType.SmallCaution);
                continue;
            }

            _statusEffects.TryAddStatusEffectDuration(target, MutedEffect, TimeSpan.FromSeconds(3));

            foreach (var (type, amount) in args.BaseDamage.DamageDict)
                args.BonusDamage.DamageDict[type] = -amount;

            _audio.PlayPredicted(GarroteSound, target, args.User);
            _popup.PopupClient(Loc.GetString("garrote-choke-self", ("target", target)), args.User, args.User, PopupType.MediumCaution);
            _popup.PopupEntity(
                Loc.GetString("garrote-choke-others", ("user", args.User), ("target", target)),
                target,
                Filter.PvsExcept(args.User),
                true,
                PopupType.MediumCaution);
            args.Handled = true;
            break;
        }
    }

    private bool TryForceSuffocateGrab(EntityUid puller, EntityUid target)
    {
        if (!TryComp<PullerComponent>(puller, out var pullerComp) || pullerComp.Pulling != target)
            return false;

        if (!TryComp<GrabIntentComponent>(puller, out var grabIntent))
            return false;

        if (!TryComp<PullableComponent>(target, out var pullable))
            return false;

        if (!TryComp<GrabbableComponent>(target, out var grabbable))
            return false;

        return _grabIntent.TrySetGrabStages(
            (puller, pullerComp, grabIntent),
            (target, pullable, grabbable),
            GrabStage.Suffocate);
    }

    private bool IsBehind(EntityUid user, EntityUid target, GarroteComponent component)
    {
        var userPos = _transform.GetWorldPosition(user);
        var targetPos = _transform.GetWorldPosition(target);
        var toUser = userPos - targetPos;

        if (toUser.LengthSquared() < 0.0001f)
            return true;

        toUser = Vector2.Normalize(toUser);
        var targetForward = _transform.GetWorldRotation(target).ToWorldVec();
        var behindDot = Vector2.Dot(toUser, -targetForward);
        return behindDot >= component.BehindDotThreshold;
    }
}
