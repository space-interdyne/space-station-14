using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._SD.Antag;

/// <summary>
/// Command jobs that may become antagonists once the server reaches the soft-command
/// player threshold. Captain, HOP and HOS are intentionally excluded.
/// </summary>
public static class SoftCommandAntagJobs
{
    public static readonly HashSet<ProtoId<JobPrototype>> Jobs =
    [
        "ChiefMedicalOfficer",
        "ResearchDirector",
        "ChiefEngineer",
        "Quartermaster",
    ];

    public static bool IsSoftCommandJob(ProtoId<JobPrototype> job)
    {
        return Jobs.Contains(job);
    }

    public static bool AllowsSoftCommandAntags(int playerCount, int minPlayers)
    {
        return minPlayers > 0 && playerCount >= minPlayers;
    }

    /// <summary>
    /// Returns the blacklist unchanged when soft-command antags are disabled,
    /// otherwise a copy with soft-command jobs removed (never mutates the prototype set).
    /// </summary>
    public static HashSet<ProtoId<JobPrototype>>? FilterJobBlacklist(
        HashSet<ProtoId<JobPrototype>>? blacklist,
        int playerCount,
        int minPlayers)
    {
        if (blacklist == null)
            return null;

        if (!AllowsSoftCommandAntags(playerCount, minPlayers))
            return blacklist;

        var filtered = new HashSet<ProtoId<JobPrototype>>(blacklist);
        filtered.ExceptWith(Jobs);
        return filtered;
    }
}
