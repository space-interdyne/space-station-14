using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._SD.SSD;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SsdCryoTeleportComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan PortalSpawnTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? TeleportTime;

    [DataField]
    public EntityUid? SourcePortal;

    [DataField]
    public EntityUid? DestinationPortal;
}
