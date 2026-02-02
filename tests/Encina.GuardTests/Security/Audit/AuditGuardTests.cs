using Encina.Security.Audit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for Encina.Security.Audit types.
/// Verifies that null arguments are properly rejected.
/// </summary>
public class AuditGuardTests
{
    #region DefaultAuditEntryFactory Guard Tests

    [Fact]
    public void DefaultAuditEntryFactory_Constructor_NullPiiMasker_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new AuditOptions());

        // Act
        var act = () => new DefaultAuditEntryFactory(null!, options);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("piiMasker");
    }

    [Fact]
    public void DefaultAuditEntryFactory_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var piiMasker = Substitute.For<IPiiMasker>();

        // Act
        var act = () => new DefaultAuditEntryFactory(piiMasker, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void DefaultAuditEntryFactory_Create_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var piiMasker = Substitute.For<IPiiMasker>();
        var options = Options.Create(new AuditOptions());
        var factory = new DefaultAuditEntryFactory(piiMasker, options);
        var context = RequestContext.CreateForTest();

        // Act
        var act = () => factory.Create<TestCommand>(null!, context, AuditOutcome.Success, null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public void DefaultAuditEntryFactory_Create_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var piiMasker = Substitute.For<IPiiMasker>();
        var options = Options.Create(new AuditOptions());
        var factory = new DefaultAuditEntryFactory(piiMasker, options);
        var request = new TestCommand();

        // Act
        var act = () => factory.Create(request, null!, AuditOutcome.Success, null);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    #endregion

    #region AuditPipelineBehavior Guard Tests

    [Fact]
    public void AuditPipelineBehavior_Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        // Arrange
        var entryFactory = Substitute.For<IAuditEntryFactory>();
        var options = Options.Create(new AuditOptions());
        var logger = Substitute.For<ILogger<AuditPipelineBehavior<TestCommand, Unit>>>();

        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            null!, entryFactory, options, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("auditStore");
    }

    [Fact]
    public void AuditPipelineBehavior_Constructor_NullEntryFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var auditStore = Substitute.For<IAuditStore>();
        var options = Options.Create(new AuditOptions());
        var logger = Substitute.For<ILogger<AuditPipelineBehavior<TestCommand, Unit>>>();

        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            auditStore, null!, options, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entryFactory");
    }

    [Fact]
    public void AuditPipelineBehavior_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var auditStore = Substitute.For<IAuditStore>();
        var entryFactory = Substitute.For<IAuditEntryFactory>();
        var logger = Substitute.For<ILogger<AuditPipelineBehavior<TestCommand, Unit>>>();

        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            auditStore, entryFactory, null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void AuditPipelineBehavior_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var auditStore = Substitute.For<IAuditStore>();
        var entryFactory = Substitute.For<IAuditEntryFactory>();
        var options = Options.Create(new AuditOptions());

        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            auditStore, entryFactory, options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region InMemoryAuditStore Guard Tests

    [Fact]
    public async Task InMemoryAuditStore_RecordAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryAuditStore();

        // Act
        var act = async () => await store.RecordAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entry");
    }

    [Fact]
    public async Task InMemoryAuditStore_GetByEntityAsync_NullEntityType_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryAuditStore();

        // Act
        var act = async () => await store.GetByEntityAsync(null!, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InMemoryAuditStore_GetByEntityAsync_EmptyEntityType_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryAuditStore();

        // Act
        var act = async () => await store.GetByEntityAsync("", null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InMemoryAuditStore_GetByUserAsync_NullUserId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryAuditStore();

        // Act
        var act = async () => await store.GetByUserAsync(null!, null, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InMemoryAuditStore_GetByUserAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryAuditStore();

        // Act
        var act = async () => await store.GetByUserAsync("", null, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InMemoryAuditStore_GetByCorrelationIdAsync_NullCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryAuditStore();

        // Act
        var act = async () => await store.GetByCorrelationIdAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InMemoryAuditStore_GetByCorrelationIdAsync_EmptyCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryAuditStore();

        // Act
        var act = async () => await store.GetByCorrelationIdAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region AuditOptions Guard Tests

    [Fact]
    public void AuditOptions_ExcludeType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        var act = () => options.ExcludeType(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestType");
    }

    [Fact]
    public void AuditOptions_IncludeQueryType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        var act = () => options.IncludeQueryType(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("queryType");
    }

    #endregion

    #region ServiceCollectionExtensions Guard Tests

    [Fact]
    public void AddEncinaAudit_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaAudit();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    #endregion

    #region AuditRetentionService Guard Tests

    [Fact]
    public void AuditRetentionService_Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new AuditOptions());
        var logger = Substitute.For<ILogger<AuditRetentionService>>();

        // Act
        var act = () => new AuditRetentionService(null!, options, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("auditStore");
    }

    [Fact]
    public void AuditRetentionService_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var auditStore = Substitute.For<IAuditStore>();
        var logger = Substitute.For<ILogger<AuditRetentionService>>();

        // Act
        var act = () => new AuditRetentionService(auditStore, null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void AuditRetentionService_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var auditStore = Substitute.For<IAuditStore>();
        var options = Options.Create(new AuditOptions());

        // Act
        var act = () => new AuditRetentionService(auditStore, options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region DefaultSensitiveDataRedactor Guard Tests

    [Fact]
    public void DefaultSensitiveDataRedactor_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DefaultSensitiveDataRedactor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void DefaultSensitiveDataRedactor_MaskForAuditGeneric_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new AuditOptions());
        var redactor = new DefaultSensitiveDataRedactor(options);

        // Act
        var act = () => redactor.MaskForAudit<TestCommand>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public void DefaultSensitiveDataRedactor_MaskForAuditObject_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new AuditOptions());
        var redactor = new DefaultSensitiveDataRedactor(options);

        // Act
        var act = () => redactor.MaskForAudit((object)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }

    #endregion

    #region InMemoryAuditStore QueryAsync Guard Tests

    [Fact]
    public async Task InMemoryAuditStore_QueryAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryAuditStore();

        // Act
        var act = async () => await store.QueryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("query");
    }

    #endregion

    #region Test Types

    // Must be public for NSubstitute to create proxies
    public sealed class TestCommand : ICommand<Unit> { }

    #endregion
}
