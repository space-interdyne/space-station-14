using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._SD.Garrote;

[RegisterComponent, NetworkedComponent]
public sealed partial class GarroteComponent : Component
{
    /// <summary>
    /// maximum angle from the victim's back for a valid attack.
    /// 0.5 ≈ 60° cone behind the target.
    /// </summary>
    [DataField]
    public float BehindDotThreshold = 0.5f;

    [DataField]
    public float BonusAsphyxiationDamage = 2f;
}

[Serializable, NetSerializable]
public enum GarroteVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum GarroteVisualState : byte
{
    Wrapped,
    Unwrapped,
}
