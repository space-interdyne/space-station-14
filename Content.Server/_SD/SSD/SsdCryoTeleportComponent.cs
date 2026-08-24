using System.Threading;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._SD.SSD;

[RegisterComponent]
public sealed partial class SsdCryoTeleportComponent : Component
{

    [DataField]
    public EntityUid? TargetCryostorage;

    [DataField]
    public EntityUid? SourcePortal;

    [DataField]
    public EntityUid? DestinationPortal;

    [ViewVariables]
    public CancellationTokenSource? TimerCancel;
}
