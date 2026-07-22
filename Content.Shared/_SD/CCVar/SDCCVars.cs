using Robust.Shared.Configuration;

namespace Content.Shared._SD.CCVar;

/// <summary>
/// Space Dream CVars. Kept separate from upstream <c>CCVars</c> per fork guidance.
/// </summary>
[CVarDefs]
public sealed class SDCCVars
{
    /// <summary>
    /// Minimum connected/ready players required before non-critical command roles may roll as antagonists
    /// </summary>
    public static readonly CVarDef<int> SoftCommandAntagMinPlayers =
        CVarDef.Create("sd.soft_command_antag_min_players", 35, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Enable or disable NSFW content (arousal alerts, ERP toys effects).
    /// </summary>
    public static readonly CVarDef<bool> NsfwContentEnabled =
        CVarDef.Create("sd.nsfw_content_enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

}
