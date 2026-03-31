using Encina.MongoDB;

namespace Encina.UnitTests.MongoDB;

/// <summary>
/// Extended tests for <see cref="MongoDbCollectionNames"/> covering all properties.
/// </summary>
public sealed class MongoDbCollectionNamesExtendedTests
{
    [Fact]
    public void AllDefaults_AreNonEmpty()
    {
        var names = new MongoDbCollectionNames();

        names.Outbox.ShouldNotBeNullOrEmpty();
        names.Inbox.ShouldNotBeNullOrEmpty();
        names.Sagas.ShouldNotBeNullOrEmpty();
        names.ScheduledMessages.ShouldNotBeNullOrEmpty();
        names.AuditLogs.ShouldNotBeNullOrEmpty();
        names.SecurityAuditEntries.ShouldNotBeNullOrEmpty();
        names.ReadAuditEntries.ShouldNotBeNullOrEmpty();
        names.TokenMappings.ShouldNotBeNullOrEmpty();
        names.RetentionPolicies.ShouldNotBeNullOrEmpty();
        names.RetentionRecords.ShouldNotBeNullOrEmpty();
        names.LegalHolds.ShouldNotBeNullOrEmpty();
        names.RetentionAuditEntries.ShouldNotBeNullOrEmpty();
        names.DataLocations.ShouldNotBeNullOrEmpty();
        names.ResidencyPolicies.ShouldNotBeNullOrEmpty();
        names.ResidencyAuditEntries.ShouldNotBeNullOrEmpty();
        names.BreachRecords.ShouldNotBeNullOrEmpty();
        names.BreachPhasedReports.ShouldNotBeNullOrEmpty();
        names.BreachAuditEntries.ShouldNotBeNullOrEmpty();
        names.Processors.ShouldNotBeNullOrEmpty();
        names.ProcessorAgreements.ShouldNotBeNullOrEmpty();
        names.ProcessorAgreementAuditEntries.ShouldNotBeNullOrEmpty();
        names.DPIAAssessments.ShouldNotBeNullOrEmpty();
        names.DPIAAuditEntries.ShouldNotBeNullOrEmpty();
        names.ABACPolicySets.ShouldNotBeNullOrEmpty();
        names.ABACPolicies.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void AllDefaults_UseSnakeCase()
    {
        var names = new MongoDbCollectionNames();

        names.Outbox.ShouldContain("_");
        names.ScheduledMessages.ShouldContain("_");
        names.SecurityAuditEntries.ShouldContain("_");
    }

    [Fact]
    public void Properties_CanBeCustomized()
    {
        var names = new MongoDbCollectionNames
        {
            Outbox = "custom_outbox",
            Inbox = "custom_inbox",
            Sagas = "custom_sagas"
        };

        names.Outbox.ShouldBe("custom_outbox");
        names.Inbox.ShouldBe("custom_inbox");
        names.Sagas.ShouldBe("custom_sagas");
    }

    [Fact]
    public void ToString_OnOptions_ReturnsDbName()
    {
        var opts = new EncinaMongoDbOptions { DatabaseName = "MyDb" };
        opts.ToString().ShouldContain("MyDb");
    }
}
