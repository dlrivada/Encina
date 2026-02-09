using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="CdcConfiguration"/> fluent builder.
/// </summary>
public sealed class CdcConfigurationTests
{
    private static readonly string[] ExpectedOrdersTables = ["Orders"];

    #region Test Helpers

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestHandler : IChangeEventHandler<TestEntity>
    {
        public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(TestEntity entity, ChangeContext context)
            => new(Right(unit));

        public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(TestEntity before, TestEntity after, ChangeContext context)
            => new(Right(unit));

        public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(TestEntity entity, ChangeContext context)
            => new(Right(unit));
    }

    #endregion

    #region UseCdc

    [Fact]
    public void UseCdc_EnablesCdcProcessing()
    {
        var config = new CdcConfiguration();

        config.UseCdc();

        config.Options.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void UseCdc_ReturnsSameInstance_ForChaining()
    {
        var config = new CdcConfiguration();

        var result = config.UseCdc();

        result.ShouldBeSameAs(config);
    }

    #endregion

    #region AddHandler

    [Fact]
    public void AddHandler_RegistersHandlerForEntityType()
    {
        var config = new CdcConfiguration();

        config.AddHandler<TestEntity, TestHandler>();

        config.HandlerRegistrations.ShouldHaveSingleItem();
        config.HandlerRegistrations[0].EntityType.ShouldBe(typeof(TestEntity));
        config.HandlerRegistrations[0].HandlerType.ShouldBe(typeof(TestHandler));
    }

    [Fact]
    public void AddHandler_ReturnsSameInstance_ForChaining()
    {
        var config = new CdcConfiguration();

        var result = config.AddHandler<TestEntity, TestHandler>();

        result.ShouldBeSameAs(config);
    }

    [Fact]
    public void AddHandler_MultipleTimes_RegistersAll()
    {
        var config = new CdcConfiguration();

        config.AddHandler<TestEntity, TestHandler>();
        config.AddHandler<TestEntity, TestHandler>();

        config.HandlerRegistrations.Count.ShouldBe(2);
    }

    #endregion

    #region WithTableMapping

    [Fact]
    public void WithTableMapping_AddsMapping()
    {
        var config = new CdcConfiguration();

        config.WithTableMapping<TestEntity>("dbo.TestEntities");

        config.TableMappings.ShouldHaveSingleItem();
        config.TableMappings[0].TableName.ShouldBe("dbo.TestEntities");
        config.TableMappings[0].EntityType.ShouldBe(typeof(TestEntity));
    }

    [Fact]
    public void WithTableMapping_ReturnsSameInstance_ForChaining()
    {
        var config = new CdcConfiguration();

        var result = config.WithTableMapping<TestEntity>("table");

        result.ShouldBeSameAs(config);
    }

    [Fact]
    public void WithTableMapping_NullTableName_ThrowsArgumentException()
    {
        var config = new CdcConfiguration();

        Should.Throw<ArgumentException>(() => config.WithTableMapping<TestEntity>(null!));
    }

    [Fact]
    public void WithTableMapping_EmptyTableName_ThrowsArgumentException()
    {
        var config = new CdcConfiguration();

        Should.Throw<ArgumentException>(() => config.WithTableMapping<TestEntity>(""));
    }

    [Fact]
    public void WithTableMapping_WhitespaceTableName_ThrowsArgumentException()
    {
        var config = new CdcConfiguration();

        Should.Throw<ArgumentException>(() => config.WithTableMapping<TestEntity>("   "));
    }

    #endregion

    #region WithOptions

    [Fact]
    public void WithOptions_AppliesConfigureAction()
    {
        var config = new CdcConfiguration();

        config.WithOptions(opts =>
        {
            opts.BatchSize = 50;
            opts.MaxRetries = 10;
        });

        config.Options.BatchSize.ShouldBe(50);
        config.Options.MaxRetries.ShouldBe(10);
    }

    [Fact]
    public void WithOptions_ReturnsSameInstance_ForChaining()
    {
        var config = new CdcConfiguration();

        var result = config.WithOptions(_ => { });

        result.ShouldBeSameAs(config);
    }

    [Fact]
    public void WithOptions_NullAction_ThrowsArgumentNullException()
    {
        var config = new CdcConfiguration();

        Should.Throw<ArgumentNullException>(() => config.WithOptions(null!));
    }

    #endregion

    #region WithMessagingBridge

    [Fact]
    public void WithMessagingBridge_EnablesMessagingBridge()
    {
        var config = new CdcConfiguration();

        config.WithMessagingBridge();

        config.Options.UseMessagingBridge.ShouldBeTrue();
        config.MessagingOptions.ShouldNotBeNull();
    }

    [Fact]
    public void WithMessagingBridge_WithConfigure_AppliesOptions()
    {
        var config = new CdcConfiguration();

        config.WithMessagingBridge(opts =>
        {
            opts.TopicPattern = "cdc.{tableName}.{operation}";
            opts.IncludeTables = ["Orders"];
        });

        config.MessagingOptions!.TopicPattern.ShouldBe("cdc.{tableName}.{operation}");
        config.MessagingOptions.IncludeTables.ShouldBe(ExpectedOrdersTables);
    }

    [Fact]
    public void WithMessagingBridge_NullConfigure_StillEnables()
    {
        var config = new CdcConfiguration();

        config.WithMessagingBridge(null);

        config.Options.UseMessagingBridge.ShouldBeTrue();
        config.MessagingOptions.ShouldNotBeNull();
    }

    [Fact]
    public void WithMessagingBridge_ReturnsSameInstance_ForChaining()
    {
        var config = new CdcConfiguration();

        var result = config.WithMessagingBridge();

        result.ShouldBeSameAs(config);
    }

    #endregion

    #region UseOutboxCdc

    [Fact]
    public void UseOutboxCdc_EnablesOutboxCdc()
    {
        var config = new CdcConfiguration();

        config.UseOutboxCdc();

        config.Options.UseOutboxCdc.ShouldBeTrue();
    }

    [Fact]
    public void UseOutboxCdc_DefaultTableName_AddsTableMapping()
    {
        var config = new CdcConfiguration();

        config.UseOutboxCdc();

        config.TableMappings.ShouldContain(m =>
            m.TableName == "OutboxMessages" && m.EntityType == typeof(JsonElement));
    }

    [Fact]
    public void UseOutboxCdc_CustomTableName_AddsTableMapping()
    {
        var config = new CdcConfiguration();

        config.UseOutboxCdc("dbo.Outbox");

        config.TableMappings.ShouldContain(m =>
            m.TableName == "dbo.Outbox" && m.EntityType == typeof(JsonElement));
    }

    [Fact]
    public void UseOutboxCdc_RegistersOutboxCdcHandler()
    {
        var config = new CdcConfiguration();

        config.UseOutboxCdc();

        config.HandlerRegistrations.ShouldContain(r =>
            r.EntityType == typeof(JsonElement));
    }

    [Fact]
    public void UseOutboxCdc_NullTableName_ThrowsArgumentException()
    {
        var config = new CdcConfiguration();

        Should.Throw<ArgumentException>(() => config.UseOutboxCdc(null!));
    }

    [Fact]
    public void UseOutboxCdc_EmptyTableName_ThrowsArgumentException()
    {
        var config = new CdcConfiguration();

        Should.Throw<ArgumentException>(() => config.UseOutboxCdc(""));
    }

    [Fact]
    public void UseOutboxCdc_ReturnsSameInstance_ForChaining()
    {
        var config = new CdcConfiguration();

        var result = config.UseOutboxCdc();

        result.ShouldBeSameAs(config);
    }

    #endregion

    #region Fluent Chaining

    [Fact]
    public void FluentChaining_AllMethods_WorkTogether()
    {
        var config = new CdcConfiguration();

        config
            .UseCdc()
            .AddHandler<TestEntity, TestHandler>()
            .WithTableMapping<TestEntity>("dbo.Tests")
            .WithOptions(opts => opts.BatchSize = 200)
            .WithMessagingBridge(opts => opts.TopicPattern = "test.{tableName}")
            .UseOutboxCdc();

        config.Options.Enabled.ShouldBeTrue();
        config.Options.BatchSize.ShouldBe(200);
        config.Options.UseMessagingBridge.ShouldBeTrue();
        config.Options.UseOutboxCdc.ShouldBeTrue();
        config.HandlerRegistrations.Count.ShouldBeGreaterThanOrEqualTo(2); // TestHandler + OutboxCdcHandler
        config.TableMappings.Count.ShouldBeGreaterThanOrEqualTo(2); // dbo.Tests + OutboxMessages
    }

    #endregion
}
