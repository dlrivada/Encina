using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Attributes;
using Encina.Compliance.Attestation.Behaviors;
using Encina.Compliance.Attestation.Model;
using Encina.Compliance.Attestation.Providers;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method

namespace Encina.ContractTests.Compliance.Attestation;

/// <summary>
/// Contract tests for <see cref="AttestationPipelineBehavior{TRequest, TResponse}"/>: the behavior
/// attests only decorated requests, only on success, and honors <see cref="AttestationFailureMode"/>.
/// Runs the real behavior against the real built-in providers.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Attestation")]
public sealed class AttestationPipelineBehaviorContractTests
{
    [AttestDecision(RecordType = "Approval")]
    private sealed record EnforcedRequest(string Name) : IRequest<string>;

    [AttestDecision(RecordType = "Approval", FailureMode = AttestationFailureMode.LogOnly)]
    private sealed record LogOnlyRequest(string Name) : IRequest<string>;

    private sealed record PlainRequest(string Name) : IRequest<string>;

    /// <summary>Delegates to a real provider and remembers every record it attested and the receipt it produced.</summary>
    private sealed class RecordingProvider(IAuditAttestationProvider inner) : IAuditAttestationProvider
    {
        public List<AuditRecord> Records { get; } = [];

        public string ProviderName => inner.ProviderName;

        public async ValueTask<Either<EncinaError, AttestationReceipt>> AttestAsync(AuditRecord record, CancellationToken ct = default)
        {
            Records.Add(record);
            return await inner.AttestAsync(record, ct);
        }

        public ValueTask<Either<EncinaError, AttestationVerification>> VerifyAsync(AttestationReceipt receipt, CancellationToken ct = default) =>
            inner.VerifyAsync(receipt, ct);

        public ValueTask<Either<EncinaError, AttestationReceipt>> GetReceiptAsync(Guid recordId, CancellationToken ct = default) =>
            inner.GetReceiptAsync(recordId, ct);

        public ValueTask<Either<EncinaError, IReadOnlyList<AttestationReceipt>>> GetReceiptsAsync(IEnumerable<Guid> recordIds, CancellationToken ct = default) =>
            inner.GetReceiptsAsync(recordIds, ct);
    }

    private readonly FakeTimeProvider _time = new();

    public static TheoryData<string> ProviderNames => new() { "InMemory", "HashChain" };

    private RecordingProvider CreateProvider(string name) => new(name switch
    {
        "InMemory" => new InMemoryAttestationProvider(_time, NullLogger<InMemoryAttestationProvider>.Instance),
        "HashChain" => new HashChainAttestationProvider(
            _time, NullLogger<HashChainAttestationProvider>.Instance, Options.Create(new HashChainOptions())),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    });

    private AttestationPipelineBehavior<TRequest, string> Behavior<TRequest>(IAuditAttestationProvider provider)
        where TRequest : IRequest<string>
        => new(provider, _time, NullLogger<AttestationPipelineBehavior<TRequest, string>>.Instance);

    private static RequestHandlerCallback<string> Next(Either<EncinaError, string> value)
        => () => ValueTask.FromResult(value);

    private static IAuditAttestationProvider FailingProvider()
    {
        var provider = Substitute.For<IAuditAttestationProvider>();
        provider.AttestAsync(Arg.Any<AuditRecord>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, AttestationReceipt>(AttestationErrors.ProviderUnavailable("Test", "down"))));
        return provider;
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task Handle_RequestWithoutAttribute_PassesThroughUnchangedAndAttestsNothing(string providerName)
    {
        var provider = CreateProvider(providerName);
        var sut = Behavior<PlainRequest>(provider);

        var result = await sut.Handle(new PlainRequest("a"), RequestContext.CreateForTest(), Next("payload"), CancellationToken.None);

        result.ShouldBe(Right<EncinaError, string>("payload"));
        provider.Records.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task Handle_DecoratedRequestSucceeds_ReturnsOriginalResultAndStoresVerifiableReceipt(string providerName)
    {
        var provider = CreateProvider(providerName);
        var sut = Behavior<EnforcedRequest>(provider);
        var context = RequestContext.CreateForTest();

        var result = await sut.Handle(new EnforcedRequest("alpha"), context, Next("done"), CancellationToken.None);

        result.ShouldBe(Right<EncinaError, string>("done"));
        var record = provider.Records.Single();
        record.RecordType.ShouldBe("Approval");
        record.CorrelationId.ShouldBe(context.CorrelationId);
        var receipt = (await provider.GetReceiptAsync(record.RecordId)).Match(r => r, _ => throw new InvalidOperationException());
        receipt.ContentHash.ShouldBe(AttestationHasher.ComputeContentHash(record));
        var verification = (await provider.VerifyAsync(receipt)).Match(v => v, _ => throw new InvalidOperationException());
        verification.IsValid.ShouldBeTrue(providerName);
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task Handle_DecoratedRequestFails_ReturnsLeftUnchangedWithoutAttesting(string providerName)
    {
        var provider = CreateProvider(providerName);
        var sut = Behavior<EnforcedRequest>(provider);
        var failure = Left<EncinaError, string>(EncinaErrors.Create("handler.failed", "boom"));

        var result = await sut.Handle(new EnforcedRequest("a"), RequestContext.CreateForTest(), Next(failure), CancellationToken.None);

        result.Match(_ => "", e => e.GetCode().IfNone("")).ShouldBe("handler.failed");
        provider.Records.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task Handle_SameDecoratedRequestTwice_CreatesIndependentReceipts(string providerName)
    {
        var provider = CreateProvider(providerName);
        var sut = Behavior<EnforcedRequest>(provider);

        await sut.Handle(new EnforcedRequest("a"), RequestContext.CreateForTest(), Next("x"), CancellationToken.None);
        await sut.Handle(new EnforcedRequest("a"), RequestContext.CreateForTest(), Next("x"), CancellationToken.None);

        provider.Records.Select(r => r.RecordId).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ProviderFailsInEnforceMode_BlocksPipelineWithProviderError()
    {
        var provider = FailingProvider();
        var sut = Behavior<EnforcedRequest>(provider);

        var result = await sut.Handle(new EnforcedRequest("a"), RequestContext.CreateForTest(), Next("done"), CancellationToken.None);

        result.Match(_ => "", e => e.GetCode().IfNone("")).ShouldBe(AttestationErrors.ProviderUnavailableCode);
        await provider.Received(1).AttestAsync(Arg.Any<AuditRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProviderFailsInLogOnlyMode_ReturnsOriginalResult()
    {
        var provider = FailingProvider();
        var sut = Behavior<LogOnlyRequest>(provider);

        var result = await sut.Handle(new LogOnlyRequest("a"), RequestContext.CreateForTest(), Next("done"), CancellationToken.None);

        result.ShouldBe(Right<EncinaError, string>("done"));
        await provider.Received(1).AttestAsync(Arg.Any<AuditRecord>(), Arg.Any<CancellationToken>());
    }
}
