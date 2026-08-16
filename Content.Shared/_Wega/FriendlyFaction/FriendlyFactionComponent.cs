using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Friendly.Faction;

[RegisterComponent]
public sealed partial class FriendlyFactionComponent : Component
{
    [DataField]
    public ProtoId<NpcFactionPrototype>? Faction;
}
