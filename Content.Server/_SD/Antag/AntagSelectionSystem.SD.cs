using Content.Server.GameTicking;
using Content.Shared.Antag;
using Content.Shared.Roles;
using Content.Shared._SD.Antag;
using Content.Shared._SD.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    [Dependency] private IConfigurationManager _sdCfg = default!;

    /// <summary>
    /// Player count used for soft-command antag eligibility.
    /// Roundstart runs before <see cref="GameRunLevel.InRound"/>, so lobby ready count is used there.
    /// </summary>
    private int GetSoftCommandAntagPlayerCount()
    {
        if (GameTicker.RunLevel != GameRunLevel.InRound)
            return GameTicker.ReadyPlayerCount();

        return GetActivePlayerCount();
    }

    private bool IsJobBlacklistedForAntag(ProtoId<JobPrototype> job, AntagSpecifierPrototype def)
    {
        if (def.JobBlacklist?.Contains(job) != true)
            return false;

        // Soft command may be antag at high pop
        if (SoftCommandAntagJobs.IsSoftCommandJob(job) &&
            SoftCommandAntagJobs.AllowsSoftCommandAntags(
                GetSoftCommandAntagPlayerCount(),
                _sdCfg.GetCVar(SDCCVars.SoftCommandAntagMinPlayers)))
        {
            return false;
        }

        return true;
    }

    private HashSet<ProtoId<JobPrototype>>? FilterSoftCommandJobBlacklist(
        HashSet<ProtoId<JobPrototype>>? blacklist)
    {
        return SoftCommandAntagJobs.FilterJobBlacklist(
            blacklist,
            GetSoftCommandAntagPlayerCount(),
            _sdCfg.GetCVar(SDCCVars.SoftCommandAntagMinPlayers));
    }
}
