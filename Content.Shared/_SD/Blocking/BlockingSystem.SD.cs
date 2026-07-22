using Content.Shared._SD.Input;
using Content.Shared.Blocking.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem
{
    private void InitializeSD()
    {
        SubscribeLocalEvent<BlockingUserComponent, RefreshMovementSpeedModifiersEvent>(OnUserRefreshMoveSpeed);

        CommandBinds.Builder
            .Bind(SDKeyFunctions.ToggleRaiseShield,
                InputCmdHandler.FromDelegate(HandleToggleRaiseShield, handle: false, outsidePrediction: false))
            .Register<BlockingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<BlockingSystem>();
    }

    private void OnUserRefreshMoveSpeed(Entity<BlockingUserComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_handQuery.TryGetComponent(entity.Owner, out var hands))
        {
            foreach (var held in _handsSystem.EnumerateHeld((entity.Owner, hands)))
            {
                if (!_blockQuery.TryComp(held, out var heldBlocking) || !heldBlocking.IsRaised)
                    continue;

                args.ModifySpeed(heldBlocking.RaisedWalkModifier, heldBlocking.RaisedSprintModifier);
                return;
            }
        }

        if (entity.Comp.BlockingItem is not { } item || !_blockQuery.TryComp(item, out var blocking))
            return;

        if (!blocking.IsRaised)
            return;

        args.ModifySpeed(blocking.RaisedWalkModifier, blocking.RaisedSprintModifier);
    }

    private void HandleToggleRaiseShield(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } user || !Exists(user))
            return;

        if (!_actionBlocker.CanInteract(user, null))
            return;

        TryToggleHeldShield(user);
    }

    /// <summary>
    /// Raises or lowers a held shield via keybind. Prefers the active hand, otherwise any held shield.
    /// </summary>
    public bool TryToggleHeldShield(EntityUid user)
    {
        if (!_handQuery.TryGetComponent(user, out var hands))
            return false;

        EntityUid? raised = null;
        EntityUid? candidate = null;

        if (_handsSystem.GetActiveItem((user, hands)) is { } active
            && _blockQuery.TryGetComponent(active, out var activeBlocking)
            && CanBlock((active, activeBlocking)))
        {
            candidate = active;
        }

        foreach (var held in _handsSystem.EnumerateHeld((user, hands)))
        {
            if (!_blockQuery.TryGetComponent(held, out var blocking) || !CanBlock((held, blocking)))
                continue;

            if (blocking.IsRaised)
                raised = held;

            candidate ??= held;
        }

        // Prefer lowering whatever is currently raised.
        if (raised != null && _blockQuery.TryGetComponent(raised.Value, out var raisedBlocking))
            return TryToggleShield((raised.Value, raisedBlocking), user);

        if (candidate == null || !_blockQuery.TryGetComponent(candidate.Value, out var candidateBlocking))
            return false;

        return TryToggleShield((candidate.Value, candidateBlocking), user);
    }
}
