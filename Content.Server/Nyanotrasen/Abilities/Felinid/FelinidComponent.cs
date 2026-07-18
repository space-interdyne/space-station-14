using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Server.Nyanotrasen.Abilities.Felinid;

[RegisterComponent]
public sealed partial class FelinidComponent : Component
{
    [DataField]
    public EntProtoId HairballPrototype = "Hairball";

    [DataField]
    public EntProtoId? HairballActionId = "ActionHairball";

    [DataField]
    public EntityUid? HairballAction;

    [DataField]
    public EntProtoId? EatActionId = "ActionEatMouse";

    [DataField]
    public EntityUid? EatAction;

    [DataField]
    public EntityUid? EatActionTarget;
}
