using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Security.Audit;

/// <summary>
/// Contract tests for <see cref="AuditPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies the behavior when auditing is disabled, when requests have no
/// [Auditable] attribute, and core delegation semantics.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Audit")]
public sealed class AuditPipelineBehaviorContractTests
{
    [Fact]
    public async Task Handle_NonAuditableRequest_DelegatesToNextWithoutAuditing()
    {
        // Arrange
        AuditPipelineBehavior<NonAuditableQuery, string>.ClearCache();

        var auditStore = Substitute.For<IAuditStore>();
        var entryFactory = Substitute.For<IAuditEntryFactory>();
        var options = Options.Create(new AuditOptions
        {
            AuditAllCommands = true,
            AuditAllQueries = false
        });
        var logger = NullLogger<AuditPipelineBehavior<NonAuditableQuery, string>>.Instance;

        var sut = new AuditPipelineBehavior<NonAuditableQuery, string>(
            auditStore, entryFactory, options, logger);

        var request = new NonAuditableQuery();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult<Either<EncinaError, string>>(Right("result"));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Handle must delegate to next and return its result");
        result.Match(
            Right: value => value.ShouldBe("result"),
            Left: _ => throw new InvalidOperationException("Should not be Left"));

        // No audit should have been recorded for a non-auditable query
        await auditStore.DidNotReceive().RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AuditableCommand_RecordsAuditEntry()
    {
        // Arrange
        AuditPipelineBehavior<AuditableCommand, Unit>.ClearCache();

        var auditStore = Substitute.For<IAuditStore>();
        auditStore.RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, LanguageExt.Unit>(LanguageExt.Unit.Default));

        var entryFactory = Substitute.For<IAuditEntryFactory>();
        entryFactory.Create(
                Arg.Any<AuditableCommand>(),
                Arg.Any<Unit>(),
                Arg.Any<IRequestContext>(),
                Arg.Any<AuditOutcome>(),
                Arg.Any<string?>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>())
            .Returns(CreateTestEntry());

        var options = Options.Create(new AuditOptions { AuditAllCommands = true });
        var logger = NullLogger<AuditPipelineBehavior<AuditableCommand, Unit>>.Instance;

        var sut = new AuditPipelineBehavior<AuditableCommand, Unit>(
            auditStore, entryFactory, options, logger);

        var request = new AuditableCommand();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> next = () =>
            ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Handle must return success from next");

        // Allow fire-and-forget audit recording to complete
        await Task.Delay(100);

        entryFactory.Received(1).Create(
            Arg.Any<AuditableCommand>(),
            Arg.Any<Unit>(),
            Arg.Any<IRequestContext>(),
            AuditOutcome.Success,
            Arg.Any<string?>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_SkippedRequest_DoesNotAudit()
    {
        // Arrange
        AuditPipelineBehavior<SkippedCommand, Unit>.ClearCache();

        var auditStore = Substitute.For<IAuditStore>();
        var entryFactory = Substitute.For<IAuditEntryFactory>();
        var options = Options.Create(new AuditOptions { AuditAllCommands = true });
        var logger = NullLogger<AuditPipelineBehavior<SkippedCommand, Unit>>.Instance;

        var sut = new AuditPipelineBehavior<SkippedCommand, Unit>(
            auditStore, entryFactory, options, logger);

        var request = new SkippedCommand();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<Unit> next = () =>
            ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));

        // Act
        var result = await sut.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await auditStore.DidNotReceive().RecordAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AuditPipelineBehavior_ImplementsIPipelineBehavior()
    {
        var auditStore = Substitute.For<IAuditStore>();
        var entryFactory = Substitute.For<IAuditEntryFactory>();
        var options = Options.Create(new AuditOptions());
        var logger = NullLogger<AuditPipelineBehavior<AuditableCommand, Unit>>.Instance;

        var sut = new AuditPipelineBehavior<AuditableCommand, Unit>(
            auditStore, entryFactory, options, logger);

        sut.ShouldBeAssignableTo<IPipelineBehavior<AuditableCommand, Unit>>(
            "AuditPipelineBehavior must implement IPipelineBehavior");
    }

    #region Helpers

    private static AuditEntry CreateTestEntry()
    {
        var now = DateTimeOffset.UtcNow;
        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid().ToString(),
            Action = "Test",
            EntityType = "TestEntity",
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now,
            CompletedAtUtc = now
        };
    }

    // Query type that is NOT audited by default (AuditAllQueries = false)
    public sealed class NonAuditableQuery : IQuery<string> { }

    // Command type that IS audited by default (AuditAllCommands = true)
    public sealed class AuditableCommand : ICommand<Unit> { }

    // Command with [Auditable(Skip = true)]
    [Auditable(Skip = true)]
    public sealed class SkippedCommand : ICommand<Unit> { }

    #endregion
}
