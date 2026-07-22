using System.Collections.Generic;
using Content.Shared._SD.Antag;
using Content.Shared.Roles;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.Tests.Shared._SD.Antag;

[TestFixture]
[TestOf(typeof(SoftCommandAntagJobs))]
public sealed class SoftCommandAntagJobsTest
{
    private static readonly ProtoId<JobPrototype> Captain = "Captain";
    private static readonly ProtoId<JobPrototype> Hop = "HeadOfPersonnel";
    private static readonly ProtoId<JobPrototype> Hos = "HeadOfSecurity";
    private static readonly ProtoId<JobPrototype> Cmo = "ChiefMedicalOfficer";
    private static readonly ProtoId<JobPrototype> Rd = "ResearchDirector";
    private static readonly ProtoId<JobPrototype> Ce = "ChiefEngineer";
    private static readonly ProtoId<JobPrototype> Qm = "Quartermaster";

    [Test]
    public void SoftJobsAreExactlyCmoRdCeQm()
    {
        Assert.That(SoftCommandAntagJobs.IsSoftCommandJob(Cmo), Is.True);
        Assert.That(SoftCommandAntagJobs.IsSoftCommandJob(Rd), Is.True);
        Assert.That(SoftCommandAntagJobs.IsSoftCommandJob(Ce), Is.True);
        Assert.That(SoftCommandAntagJobs.IsSoftCommandJob(Qm), Is.True);

        Assert.That(SoftCommandAntagJobs.IsSoftCommandJob(Captain), Is.False);
        Assert.That(SoftCommandAntagJobs.IsSoftCommandJob(Hop), Is.False);
        Assert.That(SoftCommandAntagJobs.IsSoftCommandJob(Hos), Is.False);
    }

    [Test]
    public void ThresholdUsesInclusivePlayerCount()
    {
        Assert.That(SoftCommandAntagJobs.AllowsSoftCommandAntags(34, 35), Is.False);
        Assert.That(SoftCommandAntagJobs.AllowsSoftCommandAntags(35, 35), Is.True);
        Assert.That(SoftCommandAntagJobs.AllowsSoftCommandAntags(40, 35), Is.True);
        Assert.That(SoftCommandAntagJobs.AllowsSoftCommandAntags(100, 0), Is.False);
    }

    [Test]
    public void FilterRemovesSoftJobsOnlyAtThresholdAndDoesNotMutatePrototype()
    {
        var blacklist = new HashSet<ProtoId<JobPrototype>>
        {
            Captain, Hop, Hos, Cmo, Rd, Ce, Qm,
        };

        var below = SoftCommandAntagJobs.FilterJobBlacklist(blacklist, 34, 35);
        Assert.That(below, Is.SameAs(blacklist));
        Assert.That(below!, Does.Contain(Cmo));

        var at = SoftCommandAntagJobs.FilterJobBlacklist(blacklist, 35, 35);
        Assert.That(at, Is.Not.SameAs(blacklist));
        Assert.That(at!, Does.Contain(Captain));
        Assert.That(at, Does.Contain(Hop));
        Assert.That(at, Does.Contain(Hos));
        Assert.That(at, Does.Not.Contain(Cmo));
        Assert.That(at, Does.Not.Contain(Rd));
        Assert.That(at, Does.Not.Contain(Ce));
        Assert.That(at, Does.Not.Contain(Qm));

        // prototype set must stay intact for low-pop rounds
        Assert.That(blacklist, Does.Contain(Cmo));
        Assert.That(blacklist.Count, Is.EqualTo(7));
    }
}
