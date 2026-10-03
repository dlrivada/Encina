using Encina;
using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Attributes;
using Encina.Compliance.Attestation.Behaviors;
using Encina.Compliance.Attestation.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method

namespace Encina.UnitTests.Compliance.Attestation;

[Trait("Category", "Unit")]
[Trait("Feature", "Attestation")]
public sealed class AttestationPipelineBehaviorTests
{
    private const string SentinelMessage = "SENTINEL-attestation-failure-message";

    [AttestDecision(RecordType = "ApprovalDecision")]
    private sealed record EnforcedRequest(string Name) : IRequest<string>;

    [AttestDecision(FailureMode = AttestationFailureMode.LogOnly)]
    private sealed record LogOnlyRequest(string Name) : IRequest<string>;

    private sealed record PlainRequest(string Name) : IRequest<string>;

    private readonly IAuditAttestationProvider _provider = Substitute.For<IAuditAttestationProvider>();
    private readonly FakeTimeProvider _time = new();
    private readonly CapturingLogger _logger = new();

    private AttestationPipelineBehavior<TRequest, string> Create<TRequest>()
        where TRequest : IRequest<string>
        => new(_provider, _time, new TypedLogger<TRequest>(_logger));

    private static RequestHandlerCallback<string> Next(Either<EncinaError, string> value)
        => () => ValueTask.FromResult(value);

    private void SetupAttest(Either<EncinaError, AttestationReceipt> result)
        => _provider.AttestAsync(Arg.Any<AuditRecord>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(result));

    private static AttestationReceipt Receipt() => new()
    {
        AttestationId = Guid.NewGuid(),
        AuditRecordId = Guid.NewGuid(),
        ContentHash = "hash",
        Signature = "sig",
        ProviderName = "Test",
        AttestedAtUtc = DateTimeOffset.UnixEpoch
    };

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = Create<EnforcedRequest>();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.Handle(null!, RequestContext.CreateForTest(), Next("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoAttribute_PassesThroughWithoutAttesting()
    {
        var sut = Create<PlainRequest>();

        var result = await sut.Handle(new PlainRequest("a"), RequestContext.CreateForTest(), Next("ok"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _provider.DidNotReceiveWithAnyArgs().AttestAsync(default!, default);
    }

    [Fact]
    public async Task Handle_HandlerFailed_ReturnsLeftWithoutAttesting()
    {
        var sut = Create<EnforcedRequest>();
        Either<EncinaError, string> failed = Left<EncinaError, string>(EncinaErrors.Create("handler.failed", "boom"));

        var result = await sut.Handle(new EnforcedRequest("a"), RequestContext.CreateForTest(), Next(failed), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        await _provider.DidNotReceiveWithAnyArgs().AttestAsync(default!, default);
    }

    [Fact]
    public async Task Handle_AttestationSucceeds_AttestsRecordAndReturnsResult()
    {
        SetupAttest(Right<EncinaError, AttestationReceipt>(Receipt()));
        var sut = Create<EnforcedRequest>();
        var context = RequestContext.CreateForTest();

        var result = await sut.Handle(new EnforcedRequest("alpha"), context, Next("done"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        var call = _provider.ReceivedCalls().Single();
        var record = (AuditRecord)call.GetArguments()[0]!;
        record.RecordType.ShouldBe("ApprovalDecision");
        record.OccurredAtUtc.ShouldBe(_time.GetUtcNow());
        record.CorrelationId.ShouldBe(context.CorrelationId);
        record.SerializedContent.ShouldContain("alpha");
        record.SerializedContent.ShouldContain("done");
        _logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_NoRecordType_UsesRequestTypeName()
    {
        SetupAttest(Right<EncinaError, AttestationReceipt>(Receipt()));
        var sut = Create<LogOnlyRequest>();

        await sut.Handle(new LogOnlyRequest("a"), RequestContext.CreateForTest(), Next("ok"), CancellationToken.None);

        var record = (AuditRecord)_provider.ReceivedCalls().Single().GetArguments()[0]!;
        record.RecordType.ShouldBe(nameof(LogOnlyRequest));
    }

    [Fact]
    public async Task Handle_AttestationFailsInEnforceMode_ReturnsAttestationErrorAndLogsCodeOnly()
    {
        SetupAttest(Left<EncinaError, AttestationReceipt>(EncinaErrors.Create("attest.failed", SentinelMessage)));
        var sut = Create<EnforcedRequest>();

        var result = await sut.Handle(new EnforcedRequest("a"), RequestContext.CreateForTest(), Next("ok"), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = (EncinaError)result;
        error.GetCode().IfNone(string.Empty).ShouldBe("attest.failed");
        _logger.Entries.Count.ShouldBe(1);
        _logger.Entries[0].ShouldContain("attest.failed");
        _logger.Entries[0].ShouldContain("blocked the pipeline");
        _logger.Entries[0].ShouldNotContain(SentinelMessage);
    }

    [Fact]
    public async Task Handle_AttestationFailsInLogOnlyMode_ReturnsOriginalResultAndLogsCodeOnly()
    {
        SetupAttest(Left<EncinaError, AttestationReceipt>(EncinaErrors.Create("attest.failed", SentinelMessage)));
        var sut = Create<LogOnlyRequest>();

        var result = await sut.Handle(new LogOnlyRequest("a"), RequestContext.CreateForTest(), Next("original"), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        result.IfRight(value => value.ShouldBe("original"));
        _logger.Entries.Count.ShouldBe(1);
        _logger.Entries[0].ShouldContain("LogOnly mode");
        _logger.Entries[0].ShouldNotContain(SentinelMessage);
    }

    [Fact]
    public async Task Handle_AttestationFailsWithoutCode_LogsUnknownCode()
    {
        SetupAttest(Left<EncinaError, AttestationReceipt>(EncinaError.New(SentinelMessage)));
        var sut = Create<LogOnlyRequest>();

        await sut.Handle(new LogOnlyRequest("a"), RequestContext.CreateForTest(), Next("ok"), CancellationToken.None);

        _logger.Entries.Count.ShouldBe(1);
        _logger.Entries[0].ShouldContain("encina.unknown");
        _logger.Entries[0].ShouldNotContain(SentinelMessage);
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add(formatter(state, exception));
    }

    private sealed class TypedLogger<T>(ILogger inner) : ILogger<AttestationPipelineBehavior<T, string>>
        where T : IRequest<string>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
