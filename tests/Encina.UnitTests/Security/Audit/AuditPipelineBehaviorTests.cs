using Encina.Security.Audit;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="AuditPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class AuditPipelineBehaviorTests : IDisposable
{
    private readonly IAuditStore _auditStore;
    private readonly IAuditEntryFactory _entryFactory;
    private readonly IOptions<AuditOptions> _options;
    private readonly ILogger<AuditPipelineBehavior<TestCommand, Unit>> _commandLogger;
    private readonly ILogger<AuditPipelineBehavior<TestQuery, string>> _queryLogger;

    public AuditPipelineBehaviorTests()
    {
        _auditStore = Substitute.For<IAuditStore>();
#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute internally manages the ValueTask
        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Unit.Default));
#pragma warning restore CA2012

        _entryFactory = Substitute.For<IAuditEntryFactory>();

        // Setup mock for the new 7-parameter Create method used by the behavior
        _entryFactory.Create(
            Arg.Any<object>(),
            Arg.Any<object?>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<AuditOutcome>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>())
            .Returns(CreateTestEntry());

        _options = Options.Create(new AuditOptions());

        _commandLogger = Substitute.For<ILogger<AuditPipelineBehavior<TestCommand, Unit>>>();
        _queryLogger = Substitute.For<ILogger<AuditPipelineBehavior<TestQuery, string>>>();

        // Clear static cache before each test
        ClearBehaviorCache();
    }

    public void Dispose()
    {
        ClearBehaviorCache();
        GC.SuppressFinalize(this);
    }

    private static void ClearBehaviorCache()
    {
        // Clear cache for all test types since each generic instantiation has its own static cache
        var typesToClear = new[]
        {
            typeof(AuditPipelineBehavior<TestCommand, Unit>),
            typeof(AuditPipelineBehavior<TestQuery, string>),
            typeof(AuditPipelineBehavior<AuditableQuery, string>),
            typeof(AuditPipelineBehavior<SkippedCommand, Unit>)
        };

        foreach (var behaviorType in typesToClear)
        {
            var clearCacheMethod = behaviorType.GetMethod("ClearCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            clearCacheMethod?.Invoke(null, null);
        }
    }

    private static AuditEntry CreateTestEntry() => new()
    {
        Id = Guid.NewGuid(),
        CorrelationId = $"corr-{Guid.NewGuid():N}",
        UserId = "test-user",
        TenantId = "test-tenant",
        Action = "Test",
        EntityType = "TestEntity",
        EntityId = Guid.NewGuid().ToString(),
        Outcome = AuditOutcome.Success,
        TimestampUtc = DateTime.UtcNow,
        StartedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1),
        CompletedAtUtc = DateTimeOffset.UtcNow,
        Metadata = new Dictionary<string, object?>()
    };

    #region ShouldAudit Tests

    [Fact]
    public async Task Handle_Command_WhenAuditAllCommandsTrue_ShouldAudit()
    {
        // Arrange
        var options = Options.Create(new AuditOptions { AuditAllCommands = true });
        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        // Assert
        _entryFactory.Received(1).Create(
            Arg.Any<TestCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<AuditOutcome>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_Command_WhenAuditAllCommandsFalse_ShouldNotAudit()
    {
        // Arrange
        var options = Options.Create(new AuditOptions { AuditAllCommands = false });
        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        // Assert
        _entryFactory.DidNotReceive().Create(
            Arg.Any<TestCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<AuditOutcome>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_Query_WhenAuditAllQueriesFalse_ShouldNotAudit()
    {
        // Arrange
        ClearBehaviorCache();
        var options = Options.Create(new AuditOptions { AuditAllQueries = false });
        var behavior = new AuditPipelineBehavior<TestQuery, string>(
            _auditStore, _entryFactory, options, _queryLogger);

        var request = new TestQuery();
        var context = RequestContext.CreateForTest();

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, string>>("result"), CancellationToken.None);

        // Assert
        _entryFactory.DidNotReceive().Create(
            Arg.Any<TestQuery>(),
            Arg.Any<string?>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<AuditOutcome>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_Query_WhenAuditAllQueriesTrue_ShouldAudit()
    {
        // Arrange
        ClearBehaviorCache();
        var options = Options.Create(new AuditOptions { AuditAllQueries = true });
        var behavior = new AuditPipelineBehavior<TestQuery, string>(
            _auditStore, _entryFactory, options, _queryLogger);

        var request = new TestQuery();
        var context = RequestContext.CreateForTest();

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, string>>("result"), CancellationToken.None);

        // Assert
        _entryFactory.Received(1).Create(
            Arg.Any<TestQuery>(),
            Arg.Any<string?>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<AuditOutcome>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_QueryWithAuditableAttribute_ShouldAudit()
    {
        // Arrange
        ClearBehaviorCache();
        var options = Options.Create(new AuditOptions { AuditAllQueries = false });
        var logger = Substitute.For<ILogger<AuditPipelineBehavior<AuditableQuery, string>>>();
        var behavior = new AuditPipelineBehavior<AuditableQuery, string>(
            _auditStore, _entryFactory, options, logger);

        var request = new AuditableQuery();
        var context = RequestContext.CreateForTest();

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, string>>("result"), CancellationToken.None);

        // Assert
        _entryFactory.Received(1).Create(
            Arg.Any<AuditableQuery>(),
            Arg.Any<string?>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<AuditOutcome>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_CommandWithSkipAttribute_ShouldNotAudit()
    {
        // Arrange
        ClearBehaviorCache();
        var options = Options.Create(new AuditOptions { AuditAllCommands = true });
        var logger = Substitute.For<ILogger<AuditPipelineBehavior<SkippedCommand, Unit>>>();
        var behavior = new AuditPipelineBehavior<SkippedCommand, Unit>(
            _auditStore, _entryFactory, options, logger);

        var request = new SkippedCommand();
        var context = RequestContext.CreateForTest();

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        // Assert
        _entryFactory.DidNotReceive().Create(
            Arg.Any<SkippedCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<AuditOutcome>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_ExcludedType_ShouldNotAudit()
    {
        // Arrange
        ClearBehaviorCache();
        var auditOptions = new AuditOptions { AuditAllCommands = true };
        auditOptions.ExcludeType<TestCommand>();
        var options = Options.Create(auditOptions);

        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        // Assert
        _entryFactory.DidNotReceive().Create(
            Arg.Any<TestCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            Arg.Any<AuditOutcome>(),
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    #endregion

    #region Outcome Mapping Tests

    [Fact]
    public async Task Handle_WithSuccessResult_ShouldMapToSuccessOutcome()
    {
        // Arrange
        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, _options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        // Assert
        _entryFactory.Received(1).Create(
            Arg.Any<TestCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            AuditOutcome.Success,
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_WithAuthorizationError_ShouldMapToDeniedOutcome()
    {
        // Arrange
        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, _options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();
        var error = EncinaError.New("User is not authorized to perform this action");

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(error), CancellationToken.None);

        // Assert
        _entryFactory.Received(1).Create(
            Arg.Any<TestCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            AuditOutcome.Denied,
            Arg.Is<string>(s => s.Contains("not authorized")),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_WithForbiddenError_ShouldMapToDeniedOutcome()
    {
        // Arrange
        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, _options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();
        var error = EncinaError.New("Access forbidden");

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(error), CancellationToken.None);

        // Assert
        _entryFactory.Received(1).Create(
            Arg.Any<TestCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            AuditOutcome.Denied,
            Arg.Is<string>(s => s.Contains("forbidden")),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_WithValidationError_ShouldMapToFailureOutcome()
    {
        // Arrange
        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, _options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();
        var error = EncinaError.New("Validation failed: Name is required");

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(error), CancellationToken.None);

        // Assert
        _entryFactory.Received(1).Create(
            Arg.Any<TestCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            AuditOutcome.Failure,
            Arg.Is<string>(s => s.Contains("Validation") || s.Contains("required")),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_WithBusinessError_ShouldMapToFailureOutcome()
    {
        // Arrange
        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, _options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();
        var error = EncinaError.New("Order cannot be placed");

        // Act
        await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(error), CancellationToken.None);

        // Assert
        _entryFactory.Received(1).Create(
            Arg.Any<TestCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            AuditOutcome.Failure,
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Handle_WhenExceptionThrown_ShouldRecordAndRethrow()
    {
        // Arrange
        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, _options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();

        // Act
        var act = async () => await behavior.Handle(request, context,
            () => throw new InvalidOperationException("Something went wrong"),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _entryFactory.Received(1).Create(
            Arg.Any<TestCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            AuditOutcome.Error,
            Arg.Is<string>(s => s.Contains("Something went wrong")),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_WhenOperationCancelled_ShouldRecordAndRethrow()
    {
        // Arrange
        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, _options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await behavior.Handle(request, context,
            () => throw new OperationCanceledException(),
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        _entryFactory.Received(1).Create(
            Arg.Any<TestCommand>(),
            Arg.Any<Unit?>(),
            Arg.Any<IRequestContext>(),
            AuditOutcome.Error,
            Arg.Is<string>(s => s.Contains("cancelled")),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_WhenAuditStoreFailure_ShouldNotFailRequest()
    {
        // Arrange
#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute internally manages the ValueTask
        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(EncinaError.New("Store error")));
#pragma warning restore CA2012

        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, _options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();

        // Act - Should not throw
        var result = await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAuditStoreThrows_ShouldNotFailRequest()
    {
        // Arrange
#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute internally manages the ValueTask
        _auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("Store exception"));
#pragma warning restore CA2012

        var behavior = new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, _options, _commandLogger);

        var request = new TestCommand();
        var context = RequestContext.CreateForTest();

        // Act - Should not throw
        var result = await behavior.Handle(request, context, () => new ValueTask<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        // Assert - Request should still succeed
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Constructor Guard Tests

    [Fact]
    public void Constructor_WithNullAuditStore_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            null!, _entryFactory, _options, _commandLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("auditStore");
    }

    [Fact]
    public void Constructor_WithNullEntryFactory_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, null!, _options, _commandLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entryFactory");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, null!, _commandLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new AuditPipelineBehavior<TestCommand, Unit>(
            _auditStore, _entryFactory, _options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Test Types

    // These types must be public for NSubstitute to create proxies
    public sealed class TestCommand : ICommand<Unit> { }

    public sealed class TestQuery : IQuery<string> { }

    [Auditable]
    public sealed class AuditableQuery : IQuery<string> { }

    [Auditable(Skip = true)]
    public sealed class SkippedCommand : ICommand<Unit> { }

    #endregion
}
