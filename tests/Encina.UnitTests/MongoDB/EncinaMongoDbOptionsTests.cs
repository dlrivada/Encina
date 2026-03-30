using Encina.MongoDB;

namespace Encina.UnitTests.MongoDB;

public sealed class EncinaMongoDbOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new EncinaMongoDbOptions();

        options.ConnectionString.ShouldBe("mongodb://localhost:27017");
        options.DatabaseName.ShouldBe("Encina");
        options.UseOutbox.ShouldBeFalse();
        options.UseInbox.ShouldBeFalse();
        options.UseSagas.ShouldBeFalse();
        options.UseScheduling.ShouldBeFalse();
        options.UseAuditLogStore.ShouldBeFalse();
        options.CreateIndexes.ShouldBeTrue();
        options.Collections.ShouldNotBeNull();
        options.SagaOptions.ShouldNotBeNull();
        options.SchedulingOptions.ShouldNotBeNull();
        options.ProviderHealthCheck.ShouldNotBeNull();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new EncinaMongoDbOptions
        {
            ConnectionString = "mongodb://custom:27017",
            DatabaseName = "CustomDb",
            UseOutbox = true,
            UseInbox = true,
            UseSagas = true,
            UseScheduling = true,
            UseAuditLogStore = true,
            CreateIndexes = false
        };

        options.ConnectionString.ShouldBe("mongodb://custom:27017");
        options.DatabaseName.ShouldBe("CustomDb");
        options.UseOutbox.ShouldBeTrue();
        options.UseInbox.ShouldBeTrue();
        options.UseSagas.ShouldBeTrue();
        options.UseScheduling.ShouldBeTrue();
        options.UseAuditLogStore.ShouldBeTrue();
        options.CreateIndexes.ShouldBeFalse();
    }

    [Fact]
    public void ComplianceDefaults_AreCorrect()
    {
        var options = new EncinaMongoDbOptions();

        options.UseSecurityAuditStore.ShouldBeFalse();
        options.UseReadAuditStore.ShouldBeFalse();
        options.UseAnonymization.ShouldBeFalse();
        options.UseRetention.ShouldBeFalse();
        options.UseDataResidency.ShouldBeFalse();
        options.UseBreachNotification.ShouldBeFalse();
        options.UseProcessorAgreements.ShouldBeFalse();
        options.UseABACPolicyStore.ShouldBeFalse();
    }

    [Fact]
    public void InfraDefaults_AreCorrect()
    {
        var options = new EncinaMongoDbOptions();

        options.UseModuleIsolation.ShouldBeFalse();
        options.ModuleIsolationOptions.ShouldNotBeNull();
    }

    [Fact]
    public void ComplianceProperties_CanBeSet()
    {
        var options = new EncinaMongoDbOptions
        {
            UseSecurityAuditStore = true,
            UseReadAuditStore = true,
            UseAnonymization = true,
            UseRetention = true,
            UseDataResidency = true,
            UseBreachNotification = true,
            UseProcessorAgreements = true,
            UseABACPolicyStore = true,
            UseModuleIsolation = true
        };

        options.UseSecurityAuditStore.ShouldBeTrue();
        options.UseReadAuditStore.ShouldBeTrue();
        options.UseAnonymization.ShouldBeTrue();
        options.UseRetention.ShouldBeTrue();
        options.UseDataResidency.ShouldBeTrue();
        options.UseBreachNotification.ShouldBeTrue();
        options.UseProcessorAgreements.ShouldBeTrue();
        options.UseABACPolicyStore.ShouldBeTrue();
        options.UseModuleIsolation.ShouldBeTrue();
    }
}
