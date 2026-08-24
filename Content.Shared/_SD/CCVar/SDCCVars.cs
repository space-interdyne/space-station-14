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

    /// <summary>
    /// When enabled, wish movement speed is scaled by alignment with body facing.
    /// </summary>
    public static readonly CVarDef<bool> MovementDirectionalPenaltyEnabled =
        CVarDef.Create("sd.movement.directional_penalty_enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Speed multiplier when moving perpendicular to facing (strafe).
    /// </summary>
    public static readonly CVarDef<float> MovementStrafeSpeedModifier =
        CVarDef.Create("sd.movement.strafe_speed_modifier", 0.7f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Speed multiplier when moving opposite to facing (backpedal).
    /// </summary>
    public static readonly CVarDef<float> MovementBackSpeedModifier =
        CVarDef.Create("sd.movement.back_speed_modifier", 0.5f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// After this many seconds of SSD, spawn a portal and move the body into a random empty cryo pod.
    /// </summary>
    public static readonly CVarDef<float> SsdCryoTeleportTime =
        CVarDef.Create("sd.ssd_cryo_teleport_time", 900f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// How long the SSD cryo portal stays visible before the body is teleported.
    /// </summary>
    public static readonly CVarDef<float> SsdCryoPortalDelay =
        CVarDef.Create("sd.ssd_cryo_portal_delay", 2.5f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Enable teleporting long-term SSD bodies into random cryo pods.
    /// </summary>
    public static readonly CVarDef<bool> SsdCryoTeleportEnabled =
        CVarDef.Create("sd.ssd_cryo_teleport_enabled", true, CVar.SERVER | CVar.ARCHIVE);

}
