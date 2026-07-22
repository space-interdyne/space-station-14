namespace Content.Shared.Blocking.Components;

public sealed partial class BlockingComponent
{
    /// <summary>
    /// Walk speed multiplier applied to the holder while the shield is raised.
    /// </summary>
    [DataField]
    public float RaisedWalkModifier = 0.2f;

    /// <summary>
    /// Sprint speed multiplier applied to the holder while the shield is raised.
    /// </summary>
    [DataField]
    public float RaisedSprintModifier = 0.2f;
}
