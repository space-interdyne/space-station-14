using Content.Shared._SD.Antag;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Antag;

public abstract partial class AntagSelectionSystem
{
    /// <summary>
    /// Minimum players before soft-command roles may be antag
    /// </summary>
    protected virtual int GetSoftCommandAntagMinPlayers() => int.MaxValue;

    /// <summary>
    /// Player count for soft-command eligibility
    /// </summary>
    protected virtual int GetSoftCommandAntagPlayerCount() => GetActivePlayerCount();

    private bool IsJobBlacklistedForAntag(ProtoId<JobPrototype> job, AntagSpecifierPrototype def)
    {
        if (def.JobBlacklist?.Contains(job) != true)
            return false;

        if (SoftCommandAntagJobs.IsSoftCommandJob(job) &&
            SoftCommandAntagJobs.AllowsSoftCommandAntags(
                GetSoftCommandAntagPlayerCount(),
                GetSoftCommandAntagMinPlayers()))
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
            GetSoftCommandAntagMinPlayers());
    }
}
