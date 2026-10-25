using System.Security.Cryptography;

using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Behaviors;
using Encina.Compliance.Attestation.Health;
using Encina.Compliance.Attestation.Model;
using Encina.Compliance.Attestation.Providers;
using Encina.Compliance.Attestation.Validation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Encina.GuardTests.Compliance.Attestation;

/// <summary>
/// Guard tests for the attestation members that <see cref="AttestationGuardTests"/> does not reach:
/// health check, hashers, receipt retrieval, receipt store, validator and registration.
/// Each rejection test is paired with a valid-argument test so the guarded code path also runs.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Feature", "Attestation")]
public sealed class AttestationGuardExtendedTests
{
    private static InMemoryAttestationProvider InMemory() =>
        new(new FakeTimeProvider(), NullLogger<InMemoryAttestationProvider>.Instance);

    private static HashChainAttestationProvider HashChain() =>
        new(new FakeTimeProvider(), NullLogger<HashChainAttestationProvider>.Instance, Options.Create(new HashChainOptions()));

    private static AuditRecord Record() => new()
    {
        RecordId = Guid.NewGuid(),
        RecordType = "Decision",
        OccurredAtUtc = DateTimeOffset.UnixEpoch,
        SerializedContent = "{}"
    };

    private static AttestationReceipt Receipt() => new()
    {
        AttestationId = Guid.NewGuid(),
        AuditRecordId = Guid.NewGuid(),
        ContentHash = "h",
        Signature = "s",
        ProviderName = "InMemory",
        AttestedAtUtc = DateTimeOffset.UnixEpoch
    };

    #region AttestationHealthCheck

    [Fact]
    public void HealthCheck_NullProvider_Throws()
    {
        var act = () => new AttestationHealthCheck(null!, NullLogger<AttestationHealthCheck>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("provider");
    }

    [Fact]
    public void HealthCheck_NullLogger_Throws()
    {
        var act = () => new AttestationHealthCheck(InMemory(), null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void HealthCheck_ValidArguments_Constructs() =>
        Should.NotThrow(() => new AttestationHealthCheck(InMemory(), NullLogger<AttestationHealthCheck>.Instance));

    #endregion

    #region Hashers

    [Fact]
    public void AttestationHasher_ComputeContentHash_NullRecord_Throws()
    {
        var act = () => AttestationHasher.ComputeContentHash(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("record");
    }

    [Fact]
    public void AttestationHasher_ComputeContentHashWithAlgorithm_NullRecord_Throws()
    {
        var act = () => AttestationHasher.ComputeContentHash(null!, HashAlgorithmName.SHA256);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("record");
    }

    [Fact]
    public void AttestationHasher_ComputeSha256_NullContent_Throws()
    {
        var act = () => AttestationHasher.ComputeSha256(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("content");
    }

    [Fact]
    public void AttestationHasher_ComputeHash_NullContent_Throws()
    {
        var act = () => AttestationHasher.ComputeHash(null!, HashAlgorithmName.SHA256);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("content");
    }

    [Fact]
    public void AttestationHasher_ComputeHash_UnsupportedAlgorithm_ThrowsArgumentException()
    {
        var act = () => AttestationHasher.ComputeHash("x", HashAlgorithmName.MD5);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("algorithm");
    }

    [Fact]
    public void AttestationHasher_ValidArguments_ReturnHexDigests()
    {
        AttestationHasher.ComputeContentHash(Record()).Length.ShouldBe(64);
        AttestationHasher.ComputeContentHash(Record(), HashAlgorithmName.SHA384).Length.ShouldBe(96);
        AttestationHasher.ComputeSha256("x").Length.ShouldBe(64);
        AttestationHasher.ComputeHash("x", HashAlgorithmName.SHA512).Length.ShouldBe(128);
    }

    [Fact]
    public void ContentHasher_ComputeSha256_NullContent_Throws()
    {
        var act = () => ContentHasher.ComputeSha256(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("content");
    }

    [Fact]
    public void ContentHasher_ComputeHash_NullContent_Throws()
    {
        var act = () => ContentHasher.ComputeHash(null!, HashAlgorithmName.SHA256);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("content");
    }

    [Fact]
    public void ContentHasher_UnsupportedAlgorithm_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => ContentHasher.ComputeHash("x", HashAlgorithmName.MD5))
            .ParamName.ShouldBe("algorithm");
        Should.Throw<ArgumentException>(() => ContentHasher.CreateHmac([1], HashAlgorithmName.MD5))
            .ParamName.ShouldBe("algorithm");
        Should.Throw<ArgumentException>(() => ContentHasher.GetRecommendedKeySize(HashAlgorithmName.MD5))
            .ParamName.ShouldBe("algorithm");
    }

    [Fact]
    public async Task InMemoryProvider_AttestAsync_WithActivityListener_StartsAttestationActivity()
    {
        var started = new List<string>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina.Compliance.Attestation",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStarted = activity => { lock (started) { started.Add(activity.OperationName); } }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        var provider = new InMemoryAttestationProvider(
            TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InMemoryAttestationProvider>.Instance);

        var result = await provider.AttestAsync(new AuditRecord
        {
            RecordId = Guid.NewGuid(),
            RecordType = "GuardActivity",
            OccurredAtUtc = DateTimeOffset.UnixEpoch,
            SerializedContent = "{}"
        });

        result.IsRight.ShouldBeTrue();
        lock (started) { started.ShouldContain("Attestation.Attest"); }
    }

    [Fact]
    public void HashChainOptionsValidator_NullOptions_Throws() =>
        Should.Throw<ArgumentNullException>(() => new HashChainOptionsValidator().Validate(null, null!))
            .ParamName.ShouldBe("options");

    [Fact]
    public void HashChainOptionsValidator_UnsupportedAlgorithm_FailsAndSupportedSucceeds()
    {
        var validator = new HashChainOptionsValidator();

        validator.Validate(null, new HashChainOptions { HashAlgorithm = HashAlgorithmName.MD5 }).Failed.ShouldBeTrue();
        validator.Validate(null, new HashChainOptions()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void HashChainProvider_UnsupportedAlgorithm_ThrowsArgumentException()
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new HashChainOptions { HashAlgorithm = HashAlgorithmName.MD5, HmacKey = new byte[16] });

        Should.Throw<ArgumentException>(() => new HashChainAttestationProvider(
                TimeProvider.System,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<HashChainAttestationProvider>.Instance,
                options))
            .ParamName.ShouldBe("algorithm");
    }

    #endregion

    #region Receipt retrieval and store

    [Fact]
    public async Task InMemoryProvider_GetReceiptsAsync_NullIds_Throws()
    {
        var act = () => InMemory().GetReceiptsAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("recordIds");
    }

    [Fact]
    public async Task HashChainProvider_GetReceiptsAsync_NullIds_Throws()
    {
        var act = () => HashChain().GetReceiptsAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("recordIds");
    }

    [Fact]
    public async Task InMemoryStore_StoreReceiptAsync_NullReceipt_Throws()
    {
        IAttestationReceiptStore store = InMemory();

        var act = () => store.StoreReceiptAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("receipt");
    }

    [Fact]
    public async Task InMemoryStore_GetReceiptsAsync_NullIds_Throws()
    {
        IAttestationReceiptStore store = InMemory();

        var act = () => store.GetReceiptsAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("recordIds");
    }

    [Fact]
    public async Task HashChainStore_GetReceiptsAsync_NullIds_Throws()
    {
        IAttestationReceiptStore store = HashChain();

        var act = () => store.GetReceiptsAsync(null!).AsTask();

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("recordIds");
    }

    [Fact]
    public async Task Providers_ValidArguments_AttestVerifyAndRetrieve()
    {
        foreach (IAuditAttestationProvider provider in new IAuditAttestationProvider[] { InMemory(), HashChain() })
        {
            var record = Record();

            var attested = await provider.AttestAsync(record);
            var receipt = attested.Match(r => r, _ => throw new InvalidOperationException());
            var verified = await provider.VerifyAsync(receipt);
            var fetched = await provider.GetReceiptsAsync([record.RecordId]);

            verified.Match(v => v.IsValid, _ => false).ShouldBeTrue(provider.ProviderName);
            fetched.Match(l => l.Count, _ => -1).ShouldBe(1, provider.ProviderName);
        }
    }

    #endregion

    #region Validator, pipeline behavior and registration

    [Fact]
    public void HttpOptionsValidator_NullOptions_Throws()
    {
        var act = () => new HttpAttestationOptionsValidator().Validate(null, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void HttpOptionsValidator_ValidHttpsOptions_Succeeds()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://attest.example.com/a"),
            VerifyEndpointUrl = new Uri("https://attest.example.com/v")
        };

        new HttpAttestationOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task PipelineBehavior_Handle_NullRequest_Throws()
    {
        var sut = new AttestationPipelineBehavior<FakeRequest, string>(
            InMemory(),
            new FakeTimeProvider(),
            NullLogger<AttestationPipelineBehavior<FakeRequest, string>>.Instance);

        var act = async () => await sut.Handle(
            null!, RequestContext.CreateForTest(), () => ValueTask.FromResult<LanguageExt.Either<EncinaError, string>>("ok"), CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public void UseHashChain_NullConfigure_IsAllowed() =>
        Should.NotThrow(() => new AttestationOptions().UseHashChain(null));

    [Fact]
    public void AddEncinaAttestation_ValidInMemoryConfiguration_RegistersResolvableProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAttestation(o => o.UseInMemory());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IAuditAttestationProvider>().ProviderName.ShouldBe("InMemory");
        provider.GetRequiredService<IAttestationReceiptStore>().ShouldBeSameAs(provider.GetRequiredService<IAuditAttestationProvider>());
    }

    [Fact]
    public void AddEncinaAttestation_ValidHashChainConfiguration_RegistersResolvableProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaAttestation(o => o.UseHashChain(h => h.HmacKey = RandomNumberGenerator.GetBytes(32)));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IAuditAttestationProvider>().ProviderName.ShouldBe("HashChain");
    }

    [Fact]
    public void AddEncinaAttestation_HttpWithoutAttestEndpoint_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaAttestation(o => o.UseHttp(_ => { }));

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("AttestEndpointUrl");
    }

    [Fact]
    public void AddEncinaAttestation_NoProvider_ThrowsInvalidOperation()
    {
        var act = () => new ServiceCollection().AddEncinaAttestation(_ => { });

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("No attestation provider configured");
    }

    private sealed record FakeRequest : IRequest<string>;

    #endregion
}
