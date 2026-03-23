using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Model;
using Encina.Compliance.Attestation.Providers;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Encina.ContractTests.Compliance.Attestation;

/// <summary>
/// Contract tests for <see cref="IAuditAttestationProvider"/> verifying consistent behavior
/// across all built-in implementations. Tests run against InMemory and HashChain providers.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Attestation")]
public sealed class AttestationProviderContractTests
{
    private readonly FakeTimeProvider _timeProvider = new();

    public static TheoryData<string> ProviderNames =>
        new() { "InMemory", "HashChain" };

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task AttestAsync_ValidRecord_ReturnsRight(string providerName)
    {
        var sut = CreateProvider(providerName);
        var record = CreateRecord();

        var result = await sut.AttestAsync(record);

        result.IsRight.ShouldBeTrue(
            $"AttestAsync must return Right for a valid record (provider: {providerName})");
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task AttestAsync_ReceiptContainsRequiredFields(string providerName)
    {
        var sut = CreateProvider(providerName);
        var record = CreateRecord();

        var result = await sut.AttestAsync(record);

        result.IfRight(receipt =>
        {
            receipt.AttestationId.ShouldNotBe(Guid.Empty,
                $"AttestationId must not be empty (provider: {providerName})");
            receipt.AuditRecordId.ShouldBe(record.RecordId,
                $"AuditRecordId must match the input record (provider: {providerName})");
            receipt.ContentHash.ShouldNotBeNullOrWhiteSpace(
                $"ContentHash must be populated (provider: {providerName})");
            receipt.Signature.ShouldNotBeNullOrWhiteSpace(
                $"Signature must be populated (provider: {providerName})");
            receipt.ProviderName.ShouldBe(providerName,
                $"ProviderName must match (provider: {providerName})");
        });
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task AttestAsync_SameRecord_IsIdempotent(string providerName)
    {
        var sut = CreateProvider(providerName);
        var record = CreateRecord();

        var first = await sut.AttestAsync(record);
        var second = await sut.AttestAsync(record);

        first.IsRight.ShouldBeTrue();
        second.IsRight.ShouldBeTrue();

        var r1 = first.Match(r => r, _ => throw new InvalidOperationException());
        var r2 = second.Match(r => r, _ => throw new InvalidOperationException());

        r1.AttestationId.ShouldBe(r2.AttestationId,
            $"Idempotent attestation must return same receipt (provider: {providerName})");
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task AttestAsync_DifferentRecords_ProduceDifferentSignatures(string providerName)
    {
        var sut = CreateProvider(providerName);
        var record1 = CreateRecord();
        var record2 = CreateRecord();

        var r1 = (await sut.AttestAsync(record1)).Match(r => r, _ => throw new InvalidOperationException());
        var r2 = (await sut.AttestAsync(record2)).Match(r => r, _ => throw new InvalidOperationException());

        r1.Signature.ShouldNotBe(r2.Signature,
            $"Different records must produce different signatures (provider: {providerName})");
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task VerifyAsync_ValidReceipt_ReturnsValid(string providerName)
    {
        var sut = CreateProvider(providerName);
        var record = CreateRecord();
        var receipt = (await sut.AttestAsync(record)).Match(r => r, _ => throw new InvalidOperationException());

        var result = await sut.VerifyAsync(receipt);

        result.IsRight.ShouldBeTrue();
        result.IfRight(v =>
        {
            v.IsValid.ShouldBeTrue(
                $"A valid receipt must verify successfully (provider: {providerName})");
            v.FailureReason.ShouldBeNull(
                $"No failure reason expected for valid receipt (provider: {providerName})");
        });
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task VerifyAsync_UnknownReceipt_ReturnsInvalid(string providerName)
    {
        var sut = CreateProvider(providerName);
        var unknown = new AttestationReceipt
        {
            AttestationId = Guid.NewGuid(),
            AuditRecordId = Guid.NewGuid(),
            ContentHash = "unknown",
            AttestedAtUtc = _timeProvider.GetUtcNow(),
            ProviderName = providerName,
            Signature = "unknown-sig"
        };

        var result = await sut.VerifyAsync(unknown);

        result.IsRight.ShouldBeTrue();
        result.IfRight(v =>
        {
            v.IsValid.ShouldBeFalse(
                $"Unknown receipt must fail verification (provider: {providerName})");
            v.FailureReason.ShouldNotBeNullOrWhiteSpace(
                $"Failure reason must explain why (provider: {providerName})");
        });
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task AttestAsync_SameContent_DifferentRecordIds_ProduceDifferentReceipts(string providerName)
    {
        var sut = CreateProvider(providerName);
        var content = "{\"same\":\"content\"}";

        var r1 = CreateRecord() with { SerializedContent = content };
        var r2 = CreateRecord() with { SerializedContent = content };

        var receipt1 = (await sut.AttestAsync(r1)).Match(r => r, _ => throw new InvalidOperationException());
        var receipt2 = (await sut.AttestAsync(r2)).Match(r => r, _ => throw new InvalidOperationException());

        receipt1.AttestationId.ShouldNotBe(receipt2.AttestationId,
            $"Different record IDs must produce different attestation IDs (provider: {providerName})");
    }

    private IAuditAttestationProvider CreateProvider(string name) => name switch
    {
        "InMemory" => new InMemoryAttestationProvider(
            _timeProvider,
            NullLogger<InMemoryAttestationProvider>.Instance),
        "HashChain" => new HashChainAttestationProvider(
            _timeProvider,
            NullLogger<HashChainAttestationProvider>.Instance,
            Options.Create(new HashChainOptions())),
        _ => throw new ArgumentException($"Unknown provider: {name}")
    };

    private static AuditRecord CreateRecord() => new()
    {
        RecordId = Guid.NewGuid(),
        RecordType = "ContractTest",
        OccurredAtUtc = DateTimeOffset.UtcNow,
        SerializedContent = $"{{\"id\":\"{Guid.NewGuid()}\"}}"
    };
}
