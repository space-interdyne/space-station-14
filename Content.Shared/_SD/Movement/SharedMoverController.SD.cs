using System.Numerics;
using Content.Shared._SD.CCVar;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private bool _sdDirectionalPenaltyEnabled = true;
    private float _sdStrafeSpeedModifier = 0.7f;
    private float _sdBackSpeedModifier = 0.5f;

    private void InitializeSD()
    {
        Subs.CVar(_configManager, SDCCVars.MovementDirectionalPenaltyEnabled, v => _sdDirectionalPenaltyEnabled = v, true);
        Subs.CVar(_configManager, SDCCVars.MovementStrafeSpeedModifier, v => _sdStrafeSpeedModifier = v, true);
        Subs.CVar(_configManager, SDCCVars.MovementBackSpeedModifier, v => _sdBackSpeedModifier = v, true);
    }

    /// <summary>
    /// Scales wish velocity by how aligned movement is with body facing.
    /// Forward = 1, strafe = <see cref="_sdStrafeSpeedModifier"/>, back = <see cref="_sdBackSpeedModifier"/>.
    /// </summary>
    private void ApplyDirectionalWishModifier(TransformComponent xform, ref Vector2 wishDir)
    {
        if (!_sdDirectionalPenaltyEnabled || wishDir == Vector2.Zero)
            return;

        var look = _transform.GetWorldRotation(xform).ToWorldVec();
        var move = wishDir.Normalized();
        var dot = Vector2.Dot(move, look);

        var modifier = dot >= 0f
            ? MathHelper.Lerp(_sdStrafeSpeedModifier, 1f, dot)
            : MathHelper.Lerp(_sdStrafeSpeedModifier, _sdBackSpeedModifier, -dot);

        wishDir *= modifier;
    }
}
