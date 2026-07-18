namespace Content.Server.Nyanotrasen.Abilities.Felinid;

[RegisterComponent]
public sealed partial class CoughingUpHairballComponent : Component
{
    [DataField]
    public float Accumulator;

    [DataField]
    public TimeSpan CoughUpTime = TimeSpan.FromSeconds(2.15);
}
