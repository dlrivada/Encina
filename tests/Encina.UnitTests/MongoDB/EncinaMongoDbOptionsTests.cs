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
}
