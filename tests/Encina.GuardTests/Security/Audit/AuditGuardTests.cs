using Encina.Security.Audit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for Encina.Security.Audit types.
/// Verifies that null arguments are properly rejected.
/// </summary>
public class AuditGuardTests
{
    #region DefaultOperationAuditEntryFactory Guard Tests

    [Fact]
    public void DefaultOperationAuditEntryFactory_Constructor_NullPiiMasker_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions());

        // Act
        var act = () => new DefaultOperationAuditEntryFactory(null!, options);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("piiMasker");
    }

    [Fact]
    public void DefaultOperationAuditEntryFactory_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var piiMasker = Substitute.For<IPiiMasker>();

        // Act
        var act = () => new DefaultOperationAuditEntryFactory(piiMasker, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void DefaultOperationAuditEntryFactory_Create_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var piiMasker = Substitute.For<IPiiMasker>();
        var options = Options.Create(new OperationAuditOptions());
        var factory = new DefaultOperationAuditEntryFactory(piiMasker, options);
        var context = RequestContext.CreateForTest();

        // Act
        var act = () => factory.Create<TestCommand>(null!, context, AuditOutcome.Success, null);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("request");
    }

    [Fact]
    public void DefaultOperationAuditEntryFactory_Create_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var piiMasker = Substitute.For<IPiiMasker>();
        var options = Options.Create(new OperationAuditOptions());
        var factory = new DefaultOperationAuditEntryFactory(piiMasker, options);
        var request = new TestCommand();

        // Act
        var act = () => factory.Create(request, null!, AuditOutcome.Success, null);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("context");
    }

    #endregion

    #region AuditPipelineBehavior Guard Tests

    [Fact]
    public void AuditPipelineBehavior_Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        // Arrange
        var entryFactory = Substitute.For<IOperationAuditEntryFactory>();
        var options = Options.Create(new OperationAuditOptions());
        var logger = Substitute.For<ILogger<AuditPipelineBehavior<TestCommand, Unit>>>();

        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            null!, entryFactory, options, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("auditStore");
    }

    [Fact]
    public void AuditPipelineBehavior_Constructor_NullEntryFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var auditStore = Substitute.For<IOperationAuditStore>();
        var options = Options.Create(new OperationAuditOptions());
        var logger = Substitute.For<ILogger<AuditPipelineBehavior<TestCommand, Unit>>>();

        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            auditStore, null!, options, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("entryFactory");
    }

    [Fact]
    public void AuditPipelineBehavior_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var auditStore = Substitute.For<IOperationAuditStore>();
        var entryFactory = Substitute.For<IOperationAuditEntryFactory>();
        var logger = Substitute.For<ILogger<AuditPipelineBehavior<TestCommand, Unit>>>();

        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            auditStore, entryFactory, null!, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void AuditPipelineBehavior_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var auditStore = Substitute.For<IOperationAuditStore>();
        var entryFactory = Substitute.For<IOperationAuditEntryFactory>();
        var options = Options.Create(new OperationAuditOptions());

        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            auditStore, entryFactory, options, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    #endregion

    #region InMemoryOperationAuditStore Guard Tests

    [Fact]
    public async Task InMemoryOperationAuditStore_RecordAsync_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryOperationAuditStore();

        // Act
        var act = async () => await store.RecordAsync(null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("entry");
    }

    [Fact]
    public async Task InMemoryOperationAuditStore_GetByEntityAsync_NullEntityType_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryOperationAuditStore();

        // Act
        var act = async () => await store.GetByEntityAsync(null!, null);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task InMemoryOperationAuditStore_GetByEntityAsync_EmptyEntityType_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryOperationAuditStore();

        // Act
        var act = async () => await store.GetByEntityAsync("", null);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task InMemoryOperationAuditStore_GetByUserAsync_NullUserId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryOperationAuditStore();

        // Act
        var act = async () => await store.GetByUserAsync(null!, null, null);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task InMemoryOperationAuditStore_GetByUserAsync_EmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryOperationAuditStore();

        // Act
        var act = async () => await store.GetByUserAsync("", null, null);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task InMemoryOperationAuditStore_GetByCorrelationIdAsync_NullCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryOperationAuditStore();

        // Act
        var act = async () => await store.GetByCorrelationIdAsync(null!);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task InMemoryOperationAuditStore_GetByCorrelationIdAsync_EmptyCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var store = new InMemoryOperationAuditStore();

        // Act
        var act = async () => await store.GetByCorrelationIdAsync("");

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    #endregion

    #region OperationAuditOptions Guard Tests

    [Fact]
    public void OperationAuditOptions_ExcludeType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new OperationAuditOptions();

        // Act
        var act = () => options.ExcludeType(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("requestType");
    }

    [Fact]
    public void OperationAuditOptions_IncludeQueryType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new OperationAuditOptions();

        // Act
        var act = () => options.IncludeQueryType(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("queryType");
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
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion

    #region OperationAuditRetentionService Guard Tests

    [Fact]
    public void OperationAuditRetentionService_Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions());
        var logger = Substitute.For<ILogger<OperationAuditRetentionService>>();

        // Act
        var act = () => new OperationAuditRetentionService(null!, options, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("auditStore");
    }

    [Fact]
    public void OperationAuditRetentionService_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var auditStore = Substitute.For<IOperationAuditStore>();
        var logger = Substitute.For<ILogger<OperationAuditRetentionService>>();

        // Act
        var act = () => new OperationAuditRetentionService(auditStore, null!, logger);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void OperationAuditRetentionService_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var auditStore = Substitute.For<IOperationAuditStore>();
        var options = Options.Create(new OperationAuditOptions());

        // Act
        var act = () => new OperationAuditRetentionService(auditStore, options, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    #endregion

    #region DefaultSensitiveDataRedactor Guard Tests

    [Fact]
    public void DefaultSensitiveDataRedactor_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DefaultSensitiveDataRedactor(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void DefaultSensitiveDataRedactor_MaskForAuditGeneric_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions());
        var redactor = new DefaultSensitiveDataRedactor(options);

        // Act
        var act = () => redactor.MaskForAudit<TestCommand>(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("request");
    }

    [Fact]
    public void DefaultSensitiveDataRedactor_MaskForAuditObject_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new OperationAuditOptions());
        var redactor = new DefaultSensitiveDataRedactor(options);

        // Act
        var act = () => redactor.MaskForAudit((object)null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("request");
    }

    #endregion

    #region InMemoryOperationAuditStore QueryAsync Guard Tests

    [Fact]
    public async Task InMemoryOperationAuditStore_QueryAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryOperationAuditStore();

        // Act
        var act = async () => await store.QueryAsync(null!);

        // Assert
        (await Should.ThrowAsync<ArgumentNullException>(act))
            .ParamName.ShouldBe("query");
    }

    #endregion

    #region Test Types

    // Must be public for NSubstitute to create proxies
    public sealed class TestCommand : ICommand<Unit> { }

    #endregion
}
