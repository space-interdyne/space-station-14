using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._SD.Teleportation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SyndicateTeleporterComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MinRange = 3f;

    [DataField, AutoNetworkedField]
    public float MaxRange = 8f;

    [DataField, AutoNetworkedField]
    public float SavingThrowDistance = 3f;
}

[Serializable, NetSerializable]
public enum SyndicateTeleporterVisuals : byte
{
    Charges,
}
