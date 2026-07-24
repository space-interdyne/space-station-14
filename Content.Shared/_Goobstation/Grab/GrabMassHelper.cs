// SPDX-License-Identifier: AGPL-3.0-or-later
// Local mass-ratio helper replacing Einstein ContestsSystem for grab escape.

using Robust.Shared.Physics.Components;

namespace Content.Shared._Goobstation.Grab;

public static class GrabMassHelper
{
    /// <summary>
    /// Ratio of performer mass to target mass (performer.Mass * target.InvMass), unclamped.
    /// Returns 1 when physics is missing or zero.
    /// </summary>
    public static float MassRatio(EntityManager entMan, EntityUid performer, EntityUid target)
    {
        if (!entMan.TryGetComponent(performer, out PhysicsComponent? performerPhysics)
            || !entMan.TryGetComponent(target, out PhysicsComponent? targetPhysics)
            || performerPhysics.Mass == 0
            || targetPhysics.InvMass == 0)
            return 1f;

        return performerPhysics.Mass * targetPhysics.InvMass;
    }
}
