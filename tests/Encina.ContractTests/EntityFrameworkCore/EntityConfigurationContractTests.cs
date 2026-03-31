using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Encina.ContractTests.EntityFrameworkCore;

/// <summary>
/// Contract tests that apply real entity configurations to a ModelBuilder and verify
/// the resulting model metadata (table names, keys, required properties). This exercises
/// the Configure() methods in OutboxMessageConfiguration, InboxMessageConfiguration,
/// SagaStateConfiguration, and ScheduledMessageConfiguration.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "EntityConfiguration")]
public sealed class EntityConfigurationContractTests : IDisposable
{
    private readonly ContractTestDbContext _dbContext;

    public EntityConfigurationContractTests()
    {
        var options = new DbContextOptionsBuilder<ContractTestDbContext>()
            .UseInMemoryDatabase(databaseName: $"ConfigContract_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ContractTestDbContext(options);
    }

    public void Dispose() => _dbContext.Dispose();

    // ============================
    // OutboxMessage configuration
    // ============================

    [Fact]
    public void OutboxMessage_ShouldMapToCorrectTable()
    {
        // Exercises: OutboxMessageConfiguration.Configure() - ToTable("OutboxMessages")
        var entityType = _dbContext.Model.FindEntityType(typeof(OutboxMessage));
        entityType.ShouldNotBeNull();
        entityType.GetTableName().ShouldBe("OutboxMessages");
    }

    [Fact]
    public void OutboxMessage_ShouldHaveIdAsPrimaryKey()
    {
        // Exercises: OutboxMessageConfiguration.Configure() - HasKey(x => x.Id)
        var entityType = _dbContext.Model.FindEntityType(typeof(OutboxMessage))!;
        var pk = entityType.FindPrimaryKey();
        pk.ShouldNotBeNull();
        pk.Properties.Count.ShouldBe(1);
        pk.Properties[0].Name.ShouldBe("Id");
    }

    [Fact]
    public void OutboxMessage_NotificationType_ShouldBeRequired()
    {
        // Exercises: OutboxMessageConfiguration.Configure() - Property(NotificationType).IsRequired()
        var entityType = _dbContext.Model.FindEntityType(typeof(OutboxMessage))!;
        var prop = entityType.FindProperty("NotificationType");
        prop.ShouldNotBeNull();
        prop.IsNullable.ShouldBeFalse();
        prop.GetMaxLength().ShouldBe(500);
    }

    [Fact]
    public void OutboxMessage_Content_ShouldBeRequired()
    {
        // Exercises: OutboxMessageConfiguration.Configure() - Property(Content).IsRequired()
        var entityType = _dbContext.Model.FindEntityType(typeof(OutboxMessage))!;
        var prop = entityType.FindProperty("Content");
        prop.ShouldNotBeNull();
        prop.IsNullable.ShouldBeFalse();
    }

    // ============================
    // InboxMessage configuration
    // ============================

    [Fact]
    public void InboxMessage_ShouldMapToCorrectTable()
    {
        // Exercises: InboxMessageConfiguration.Configure() - ToTable("InboxMessages")
        var entityType = _dbContext.Model.FindEntityType(typeof(InboxMessage));
        entityType.ShouldNotBeNull();
        entityType.GetTableName().ShouldBe("InboxMessages");
    }

    [Fact]
    public void InboxMessage_ShouldHaveMessageIdAsPrimaryKey()
    {
        // Exercises: InboxMessageConfiguration.Configure() - HasKey(x => x.MessageId)
        var entityType = _dbContext.Model.FindEntityType(typeof(InboxMessage))!;
        var pk = entityType.FindPrimaryKey();
        pk.ShouldNotBeNull();
        pk.Properties.Count.ShouldBe(1);
        pk.Properties[0].Name.ShouldBe("MessageId");
    }

    [Fact]
    public void InboxMessage_RequestType_ShouldBeRequired()
    {
        // Exercises: InboxMessageConfiguration.Configure() - Property(RequestType).IsRequired()
        var entityType = _dbContext.Model.FindEntityType(typeof(InboxMessage))!;
        var prop = entityType.FindProperty("RequestType");
        prop.ShouldNotBeNull();
        prop.IsNullable.ShouldBeFalse();
        prop.GetMaxLength().ShouldBe(500);
    }

    [Fact]
    public void InboxMessage_ExpiresAtUtc_ShouldBeRequired()
    {
        // Exercises: InboxMessageConfiguration.Configure() - Property(ExpiresAtUtc).IsRequired()
        var entityType = _dbContext.Model.FindEntityType(typeof(InboxMessage))!;
        var prop = entityType.FindProperty("ExpiresAtUtc");
        prop.ShouldNotBeNull();
        prop.IsNullable.ShouldBeFalse();
    }

    // ============================
    // SagaState configuration
    // ============================

    [Fact]
    public void SagaState_ShouldMapToCorrectTable()
    {
        // Exercises: SagaStateConfiguration.Configure() - ToTable("SagaStates")
        var entityType = _dbContext.Model.FindEntityType(typeof(SagaState));
        entityType.ShouldNotBeNull();
        entityType.GetTableName().ShouldBe("SagaStates");
    }

    [Fact]
    public void SagaState_ShouldHaveSagaIdAsPrimaryKey()
    {
        // Exercises: SagaStateConfiguration.Configure() - HasKey(x => x.SagaId)
        var entityType = _dbContext.Model.FindEntityType(typeof(SagaState))!;
        var pk = entityType.FindPrimaryKey();
        pk.ShouldNotBeNull();
        pk.Properties.Count.ShouldBe(1);
        pk.Properties[0].Name.ShouldBe("SagaId");
    }

    [Fact]
    public void SagaState_SagaType_ShouldBeRequired()
    {
        // Exercises: SagaStateConfiguration.Configure() - Property(SagaType).IsRequired()
        var entityType = _dbContext.Model.FindEntityType(typeof(SagaState))!;
        var prop = entityType.FindProperty("SagaType");
        prop.ShouldNotBeNull();
        prop.IsNullable.ShouldBeFalse();
        prop.GetMaxLength().ShouldBe(500);
    }

    [Fact]
    public void SagaState_Status_ShouldBeConfiguredAsRequired()
    {
        // Exercises: SagaStateConfiguration.Configure() - Property(Status).IsRequired().HasConversion<string>()
        var entityType = _dbContext.Model.FindEntityType(typeof(SagaState))!;
        var prop = entityType.FindProperty("Status");
        prop.ShouldNotBeNull();
        prop.IsNullable.ShouldBeFalse();
        prop.GetMaxLength().ShouldBe(50);
    }

    [Fact]
    public void SagaState_Data_ShouldBeRequired()
    {
        // Exercises: SagaStateConfiguration.Configure() - Property(Data).IsRequired()
        var entityType = _dbContext.Model.FindEntityType(typeof(SagaState))!;
        var prop = entityType.FindProperty("Data");
        prop.ShouldNotBeNull();
        prop.IsNullable.ShouldBeFalse();
    }

    // ============================
    // ScheduledMessage configuration
    // ============================

    [Fact]
    public void ScheduledMessage_ShouldMapToCorrectTable()
    {
        // Exercises: ScheduledMessageConfiguration.Configure() - ToTable("ScheduledMessages")
        var entityType = _dbContext.Model.FindEntityType(typeof(ScheduledMessage));
        entityType.ShouldNotBeNull();
        entityType.GetTableName().ShouldBe("ScheduledMessages");
    }

    [Fact]
    public void ScheduledMessage_ShouldHaveIdAsPrimaryKey()
    {
        // Exercises: ScheduledMessageConfiguration.Configure() - HasKey(x => x.Id)
        var entityType = _dbContext.Model.FindEntityType(typeof(ScheduledMessage))!;
        var pk = entityType.FindPrimaryKey();
        pk.ShouldNotBeNull();
        pk.Properties.Count.ShouldBe(1);
        pk.Properties[0].Name.ShouldBe("Id");
    }

    [Fact]
    public void ScheduledMessage_RequestType_ShouldBeRequired()
    {
        // Exercises: ScheduledMessageConfiguration.Configure() - Property(RequestType).IsRequired()
        var entityType = _dbContext.Model.FindEntityType(typeof(ScheduledMessage))!;
        var prop = entityType.FindProperty("RequestType");
        prop.ShouldNotBeNull();
        prop.IsNullable.ShouldBeFalse();
        prop.GetMaxLength().ShouldBe(500);
    }

    [Fact]
    public void ScheduledMessage_IsRecurring_ShouldDefaultToFalse()
    {
        // Exercises: ScheduledMessageConfiguration.Configure() - Property(IsRecurring).HasDefaultValue(false)
        var entityType = _dbContext.Model.FindEntityType(typeof(ScheduledMessage))!;
        var prop = entityType.FindProperty("IsRecurring");
        prop.ShouldNotBeNull();
        prop.GetDefaultValue().ShouldBe(false);
    }

    [Fact]
    public void ScheduledMessage_CronExpression_ShouldBeOptionalWithMaxLength()
    {
        // Exercises: ScheduledMessageConfiguration.Configure() - CronExpression IsRequired(false) + MaxLength(255)
        var entityType = _dbContext.Model.FindEntityType(typeof(ScheduledMessage))!;
        var prop = entityType.FindProperty("CronExpression");
        prop.ShouldNotBeNull();
        prop.IsNullable.ShouldBeTrue();
        prop.GetMaxLength().ShouldBe(255);
    }
}
