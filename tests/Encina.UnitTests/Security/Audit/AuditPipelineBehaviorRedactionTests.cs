#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// The audit entry of a failed request carries the error code or exception type, never a message (#1557).
/// </summary>
public sealed class AuditPipelineBehaviorRedactionTests
{
    private const string Sentinel = "SENTINEL-audit-error-message";
    private const string Code = "audit.sentinel.code";

    private readonly IOperationAuditStore _auditStore = Substitute.For<IOperationAuditStore>();
    private readonly IOperationAuditEntryFactory _entryFactory = Substitute.For<IOperationAuditEntryFactory>();
    private string? _recordedErrorMessage;
    private int _createCalls;

    public AuditPipelineBehaviorRedactionTests()
    {
        _auditStore.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Unit.Default));
        _entryFactory.Create(
                Arg.Any<object>(), Arg.Any<object?>(), Arg.Any<IRequestContext>(), Arg.Any<AuditOutcome>(),
                Arg.Do<string?>(m => { _recordedErrorMessage = m; _createCalls++; }),
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
            .Returns(_ => new OperationAuditEntry
            {
                Id = Guid.NewGuid(),
                CorrelationId = "corr",
                Action = "Test",
                EntityType = "TestEntity",
                Outcome = AuditOutcome.Failure,
                TimestampUtc = DateTime.UtcNow,
                StartedAtUtc = DateTimeOffset.UtcNow,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object?>()
            });
    }

    private AuditPipelineBehavior<RedactionCommand, Unit> CreateSut() =>
        new(_auditStore, _entryFactory, Options.Create(new OperationAuditOptions { AuditAllCommands = true }),
            NullLogger<AuditPipelineBehavior<RedactionCommand, Unit>>.Instance);

    [Fact]
    public async Task Handle_LeftResult_RecordsTheErrorCodeNotTheMessage()
    {
        var sut = CreateSut();

        await sut.Handle(new RedactionCommand(), RequestContext.CreateForTest(),
            () => new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(EncinaErrors.Create(Code, Sentinel))),
            CancellationToken.None);

        _createCalls.ShouldBe(1);
        _recordedErrorMessage.ShouldBe(Code);
    }

    [Fact]
    public async Task Handle_HandlerThrows_RecordsTheExceptionTypeNotTheMessage()
    {
        var sut = CreateSut();

        await Should.ThrowAsync<InvalidOperationException>(() => sut.Handle(
            new RedactionCommand(), RequestContext.CreateForTest(),
            () => throw new InvalidOperationException(Sentinel),
            CancellationToken.None).AsTask());

        _createCalls.ShouldBe(1);
        _recordedErrorMessage.ShouldBe(nameof(InvalidOperationException));
    }

    public sealed class RedactionCommand : ICommand<Unit> { }
}
