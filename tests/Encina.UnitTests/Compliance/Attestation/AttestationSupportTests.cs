using System.Diagnostics;
using System.Security.Cryptography;

using Encina.Compliance.Attestation;
using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Attributes;
using Encina.Compliance.Attestation.Diagnostics;
using Encina.Compliance.Attestation.Model;
using Encina.Compliance.Attestation.Providers;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Encina.UnitTests.Compliance.Attestation;

/// <summary>
/// Behavior tests for the attestation helpers: errors, hasher, diagnostics, options, models
/// and the receipt retrieval members of the built-in providers.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Attestation")]
public sealed class AttestationSupportTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 5, 6, 7, 8, 9, TimeSpan.Zero));

    private static AuditRecord Record(string content = "{\"k\":\"v\"}") => new()
    {
        RecordId = Guid.NewGuid(),
        RecordType = "Decision",
        OccurredAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        SerializedContent = content
    };

    private InMemoryAttestationProvider InMemory() =>
        new(_time, NullLogger<InMemoryAttestationProvider>.Instance);

    private HashChainAttestationProvider HashChain() =>
        new(_time, NullLogger<HashChainAttestationProvider>.Instance, Options.Create(new HashChainOptions()));

    #region AttestationErrors

    public static TheoryData<Func<EncinaError>, string> ErrorFactories => new()
    {
        { () => AttestationErrors.VerificationFailed(Guid.NewGuid(), "p", "r"), AttestationErrors.VerificationFailedCode },
        { () => AttestationErrors.ProviderUnavailable("p", "r"), AttestationErrors.ProviderUnavailableCode },
        { () => AttestationErrors.ContentHashMismatch(Guid.NewGuid(), "p"), AttestationErrors.ContentHashMismatchCode },
        { () => AttestationErrors.ChainIntegrityBroken("p", 3), AttestationErrors.ChainIntegrityBrokenCode },
        { () => AttestationErrors.HttpEndpointError(new Uri("https://e.example/x"), 502, "body"), AttestationErrors.HttpEndpointErrorCode },
        { () => AttestationErrors.HttpResponseTooLarge("p", 10, 5), AttestationErrors.HttpResponseTooLargeCode },
        { () => AttestationErrors.ReceiptNotFound(Guid.NewGuid(), "p"), AttestationErrors.ReceiptNotFoundCode },
    };

    [Theory]
    [MemberData(nameof(ErrorFactories))]
    public void ErrorFactory_ProducesErrorWithItsCodeAndAttestationStage(Func<EncinaError> factory, string expectedCode)
    {
        var error = factory();

        error.GetCode().IfNone("").ShouldBe(expectedCode);
        expectedCode.ShouldStartWith("attestation.");
        error.GetDetails()["stage"].ShouldBe("attestation");
    }

    [Fact]
    public void VerificationFailed_CarriesRecordIdProviderAndReason()
    {
        var id = Guid.NewGuid();

        var error = AttestationErrors.VerificationFailed(id, "HashChain", "tampered");

        error.Message.ShouldContain(id.ToString());
        error.GetDetails()["recordId"].ShouldBe(id.ToString());
        error.GetDetails()["provider"].ShouldBe("HashChain");
        error.GetDetails()["reason"].ShouldBe("tampered");
    }

    [Fact]
    public void ChainIntegrityBroken_CarriesBrokenIndex()
    {
        var error = AttestationErrors.ChainIntegrityBroken("HashChain", 7);

        error.GetDetails()["brokenAtIndex"].ShouldBe(7);
        error.Message.ShouldContain("index 7");
    }

    [Fact]
    public void HttpResponseTooLarge_CarriesObservedAndMaximumSizes()
    {
        var error = AttestationErrors.HttpResponseTooLarge("Http", 2_000_000, 1_048_576);

        error.GetDetails()["contentLength"].ShouldBe(2_000_000L);
        error.GetDetails()["maxBytes"].ShouldBe(1_048_576L);
    }

    [Fact]
    public void HttpEndpointError_CarriesUrlStatusAndExcerpt()
    {
        var error = AttestationErrors.HttpEndpointError(new Uri("https://e.example/attest"), 429, "slow down");

        error.GetDetails()["endpointUrl"].ShouldBe("https://e.example/attest");
        error.GetDetails()["statusCode"].ShouldBe(429);
        error.GetDetails()["responseExcerpt"].ShouldBe("slow down");
    }

    #endregion

    #region AttestationHasher

    [Fact]
    public void ComputeContentHash_MatchesSha256OfSerializedContent()
    {
        var record = Record("hello");

        var hash = AttestationHasher.ComputeContentHash(record);

        hash.ShouldBe(Convert.ToHexStringLower(SHA256.HashData("hello"u8.ToArray())));
    }

    [Fact]
    public void ComputeContentHash_WithAlgorithm_UsesRequestedDigestLength()
    {
        var record = Record("hello");

        AttestationHasher.ComputeContentHash(record, HashAlgorithmName.SHA384).Length.ShouldBe(96);
        AttestationHasher.ComputeContentHash(record, HashAlgorithmName.SHA512).Length.ShouldBe(128);
    }

    [Fact]
    public void ComputeContentHash_UnsupportedAlgorithm_Throws() =>
        Should.Throw<ArgumentException>(() =>
            AttestationHasher.ComputeContentHash(Record(), HashAlgorithmName.MD5));

    [Fact]
    public void ComputeSha256_IsDeterministicAndContentSensitive()
    {
        AttestationHasher.ComputeSha256("a").ShouldBe(AttestationHasher.ComputeSha256("a"));
        AttestationHasher.ComputeSha256("a").ShouldNotBe(AttestationHasher.ComputeSha256("b"));
    }

    [Fact]
    public void ComputeHash_Sha256_EqualsComputeSha256() =>
        AttestationHasher.ComputeHash("abc", HashAlgorithmName.SHA256)
            .ShouldBe(AttestationHasher.ComputeSha256("abc"));

    #endregion

    #region AttestationDiagnostics

    // Tests run in parallel and the ActivitySource is process-wide: only record activities started
    // from the execution context of the test that attached the capture list.
    private static readonly AsyncLocal<List<Activity>?> Capture = new();

    private static ActivityListener ListenToAttestation(List<Activity> started)
    {
        Capture.Value = started;
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AttestationDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => Capture.Value?.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public void StartAttestation_WithListener_CreatesActivityTaggedWithProviderAndRecordType()
    {
        var started = new List<Activity>();
        using var listener = ListenToAttestation(started);

        using var activity = AttestationDiagnostics.StartAttestation("InMemory", "Decision");

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Attestation.Attest");
        activity.GetTagItem(AttestationDiagnostics.TagProviderName).ShouldBe("InMemory");
        activity.GetTagItem(AttestationDiagnostics.TagRecordType).ShouldBe("Decision");
    }

    [Fact]
    public void StartVerification_WithListener_CreatesActivityTaggedWithProvider()
    {
        var started = new List<Activity>();
        using var listener = ListenToAttestation(started);

        using var activity = AttestationDiagnostics.StartVerification("HashChain");

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Attestation.Verify");
        activity.GetTagItem(AttestationDiagnostics.TagProviderName).ShouldBe("HashChain");
    }

    [Fact]
    public void RecordSuccess_SetsOutcomeTagAndOkStatus()
    {
        var started = new List<Activity>();
        using var listener = ListenToAttestation(started);
        using var activity = AttestationDiagnostics.StartVerification("X");

        AttestationDiagnostics.RecordSuccess(activity);

        activity!.GetTagItem(AttestationDiagnostics.TagOutcome).ShouldBe("success");
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public void RecordFailure_SetsOutcomeTagAndErrorStatusWithReason()
    {
        var started = new List<Activity>();
        using var listener = ListenToAttestation(started);
        using var activity = AttestationDiagnostics.StartVerification("X");

        AttestationDiagnostics.RecordFailure(activity, "Record not found");

        activity!.GetTagItem(AttestationDiagnostics.TagOutcome).ShouldBe("failure");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Record not found");
    }

    [Fact]
    public void RecordSuccessAndFailure_NullActivity_DoNotThrow()
    {
        Should.NotThrow(() => AttestationDiagnostics.RecordSuccess(null));
        Should.NotThrow(() => AttestationDiagnostics.RecordFailure(null, "reason"));
    }

    [Fact]
    public async Task InMemoryAttestAndVerify_WithListener_EmitAttestAndVerifyActivities()
    {
        var started = new List<Activity>();
        using var listener = ListenToAttestation(started);
        var sut = InMemory();
        var record = Record();

        var attested = await sut.AttestAsync(record);
        var receipt = attested.Match(r => r, _ => throw new InvalidOperationException());
        await sut.VerifyAsync(receipt);

        started.Select(a => a.OperationName).ShouldBe(["Attestation.Attest", "Attestation.Verify"]);
        started[0].GetTagItem(AttestationDiagnostics.TagOutcome).ShouldBe("success");
    }

    [Fact]
    public async Task HashChainAttestAndVerify_WithListener_EmitAttestAndVerifyActivities()
    {
        var started = new List<Activity>();
        using var listener = ListenToAttestation(started);
        var sut = HashChain();

        var attested = await sut.AttestAsync(Record());
        var receipt = attested.Match(r => r, _ => throw new InvalidOperationException());
        var verified = await sut.VerifyAsync(receipt);

        verified.Match(v => v.IsValid, _ => false).ShouldBeTrue();
        started.Select(a => a.OperationName).ShouldBe(["Attestation.Attest", "Attestation.Verify"]);
    }

    [Fact]
    public async Task InMemoryVerify_UnknownRecord_WithListener_MarksActivityAsFailed()
    {
        var started = new List<Activity>();
        using var listener = ListenToAttestation(started);
        var sut = InMemory();
        var receipt = new AttestationReceipt
        {
            AttestationId = Guid.NewGuid(),
            AuditRecordId = Guid.NewGuid(),
            ContentHash = "h",
            Signature = "s",
            ProviderName = "InMemory",
            AttestedAtUtc = _time.GetUtcNow()
        };

        await sut.VerifyAsync(receipt);

        started.Single().GetTagItem(AttestationDiagnostics.TagOutcome).ShouldBe("failure");
    }

    #endregion

    #region Receipt retrieval

    [Fact]
    public async Task InMemoryGetReceiptAsync_KnownRecord_ReturnsStoredReceipt()
    {
        var sut = InMemory();
        var record = Record();
        var attested = (await sut.AttestAsync(record)).Match(r => r, _ => throw new InvalidOperationException());

        var found = (await sut.GetReceiptAsync(record.RecordId)).Match(r => r, _ => throw new InvalidOperationException());

        found.ShouldBe(attested);
    }

    [Fact]
    public async Task InMemoryGetReceiptAsync_UnknownRecord_ReturnsReceiptNotFound()
    {
        var sut = InMemory();

        var result = await sut.GetReceiptAsync(Guid.NewGuid());

        result.Match(_ => "", e => e.GetCode().IfNone("")).ShouldBe(AttestationErrors.ReceiptNotFoundCode);
    }

    [Fact]
    public async Task InMemoryGetReceiptsAsync_ReturnsOnlyKnownRecords()
    {
        var sut = InMemory();
        var record = Record();
        await sut.AttestAsync(record);

        var result = (await sut.GetReceiptsAsync([record.RecordId, Guid.NewGuid()]))
            .Match(r => r, _ => throw new InvalidOperationException());

        result.Select(r => r.AuditRecordId).ShouldBe([record.RecordId]);
    }

    [Fact]
    public async Task InMemoryReceiptStore_StoreThenGet_RoundTripsAndFirstWriteWins()
    {
        var sut = InMemory();
        IAttestationReceiptStore store = sut;
        var id = Guid.NewGuid();
        var first = StoredReceipt(id, "first");
        var second = StoredReceipt(id, "second");

        await store.StoreReceiptAsync(first);
        await store.StoreReceiptAsync(second);
        var found = await store.GetReceiptAsync(id);
        var many = await store.GetReceiptsAsync([id, Guid.NewGuid()]);

        found.ShouldBe(first);
        many.ShouldBe([first]);
    }

    [Fact]
    public async Task InMemoryReceiptStore_GetUnknown_ReturnsNull()
    {
        IAttestationReceiptStore store = InMemory();

        (await store.GetReceiptAsync(Guid.NewGuid())).ShouldBeNull();
        (await store.GetReceiptsAsync([Guid.NewGuid()])).ShouldBeEmpty();
    }

    [Fact]
    public async Task HashChainGetReceiptAsync_KnownRecord_ReturnsStoredReceipt()
    {
        var sut = HashChain();
        var record = Record();
        var attested = (await sut.AttestAsync(record)).Match(r => r, _ => throw new InvalidOperationException());

        var found = (await sut.GetReceiptAsync(record.RecordId)).Match(r => r, _ => throw new InvalidOperationException());

        found.ShouldBe(attested);
    }

    [Fact]
    public async Task HashChainGetReceiptAsync_UnknownRecord_ReturnsReceiptNotFound()
    {
        var result = await HashChain().GetReceiptAsync(Guid.NewGuid());

        result.Match(_ => "", e => e.GetCode().IfNone("")).ShouldBe(AttestationErrors.ReceiptNotFoundCode);
    }

    [Fact]
    public async Task HashChainGetReceiptsAsync_ReturnsOnlyKnownRecordsInRequestOrder()
    {
        var sut = HashChain();
        var a = Record("a");
        var b = Record("b");
        await sut.AttestAsync(a);
        await sut.AttestAsync(b);

        var result = (await sut.GetReceiptsAsync([b.RecordId, Guid.NewGuid(), a.RecordId]))
            .Match(r => r, _ => throw new InvalidOperationException());

        result.Select(r => r.AuditRecordId).ShouldBe([b.RecordId, a.RecordId]);
    }

    [Fact]
    public async Task HashChainReceiptStore_GetAndGetMany_ReflectAttestedReceipts()
    {
        var sut = HashChain();
        IAttestationReceiptStore store = sut;
        var record = Record();
        var attested = (await sut.AttestAsync(record)).Match(r => r, _ => throw new InvalidOperationException());

        (await store.GetReceiptAsync(record.RecordId)).ShouldBe(attested);
        (await store.GetReceiptAsync(Guid.NewGuid())).ShouldBeNull();
        (await store.GetReceiptsAsync([record.RecordId, Guid.NewGuid()])).ShouldBe([attested]);
    }

    [Fact]
    public async Task HashChainReceiptStore_StoreReceipt_IsIgnoredBecauseChainOwnsStorage()
    {
        var sut = HashChain();
        IAttestationReceiptStore store = sut;
        var foreign = StoredReceipt(Guid.NewGuid(), "foreign");

        await store.StoreReceiptAsync(foreign);

        (await store.GetReceiptAsync(foreign.AuditRecordId)).ShouldBeNull();
    }

    private static AttestationReceipt StoredReceipt(Guid recordId, string signature) => new()
    {
        AttestationId = Guid.NewGuid(),
        AuditRecordId = recordId,
        ContentHash = "h",
        Signature = signature,
        ProviderName = "InMemory",
        AttestedAtUtc = DateTimeOffset.UnixEpoch
    };

    #endregion

    #region Options

    [Fact]
    public void HttpAttestationOptions_ToString_RedactsAuthHeaderAndKeepsEndpoints()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://a.example/attest"),
            VerifyEndpointUrl = new Uri("https://a.example/verify"),
            AuthHeader = "Bearer super-secret-token",
            AllowInsecureHttp = true
        };

        var text = options.ToString();

        text.ShouldNotContain("super-secret-token");
        text.ShouldContain("[REDACTED]");
        text.ShouldContain("https://a.example/attest");
        text.ShouldContain("https://a.example/verify");
        text.ShouldContain("AllowInsecureHttp = True");
    }

    [Fact]
    public void HttpAttestationOptions_ToString_WithoutAuthHeader_ShowsNull()
    {
        var options = new HttpAttestationOptions { AttestEndpointUrl = new Uri("https://a.example/attest") };

        options.ToString().ShouldContain("AuthHeader = null");
    }

    [Fact]
    public void HttpAttestationOptions_Serialization_ExcludesAuthHeader()
    {
        var options = new HttpAttestationOptions
        {
            AttestEndpointUrl = new Uri("https://a.example/attest"),
            AuthHeader = "Bearer super-secret-token"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(options);

        json.ShouldNotContain("super-secret-token");
        json.ShouldNotContain("AuthHeader");
    }

    [Fact]
    public void HashChainOptions_Defaults_AreSha256WithoutKeyOrPath()
    {
        var options = new HashChainOptions();

        options.HashAlgorithm.ShouldBe(HashAlgorithmName.SHA256);
        options.HmacKey.ShouldBeNull();
        options.StoragePath.ShouldBeNull();
    }

    [Fact]
    public void AttestationOptions_UseHashChain_AppliesConfigureDelegate()
    {
        var options = new AttestationOptions();
        var key = new byte[] { 1, 2, 3 };

        var returned = options.UseHashChain(o => { o.HmacKey = key; o.HashAlgorithm = HashAlgorithmName.SHA512; });

        returned.ShouldBeSameAs(options);
        options.AddHealthCheck.ShouldBeFalse();
    }

    #endregion

    #region Models and attribute

    [Fact]
    public void AuditRecord_OptionalProperties_DefaultToNull()
    {
        var record = Record();

        record.CorrelationId.ShouldBeNull();
        record.ActorId.ShouldBeNull();
        record.TenantId.ShouldBeNull();
        record.ModuleId.ShouldBeNull();
        record.Metadata.ShouldBeNull();
    }

    [Fact]
    public void AuditRecord_With_ChangesOnlyTheRequestedProperty()
    {
        var record = Record();

        var changed = record with { ActorId = "alice", TenantId = "t1", ModuleId = "m1" };

        changed.ActorId.ShouldBe("alice");
        changed.TenantId.ShouldBe("t1");
        changed.ModuleId.ShouldBe("m1");
        changed.RecordId.ShouldBe(record.RecordId);
        changed.ShouldNotBe(record);
    }

    [Fact]
    public void AttestationReceipt_Equality_IsValueBased()
    {
        var id = Guid.NewGuid();
        var at = DateTimeOffset.UnixEpoch;
        AttestationReceipt Make() => new()
        {
            AttestationId = id,
            AuditRecordId = id,
            ContentHash = "h",
            Signature = "s",
            ProviderName = "P",
            AttestedAtUtc = at
        };

        Make().ShouldBe(Make());
        (Make() with { ExpiresAtUtc = at.AddDays(1) }).ShouldNotBe(Make());
    }

    [Fact]
    public void AttestDecisionAttribute_Defaults_EnforceWithNoReasonOrRecordType()
    {
        var attribute = new AttestDecisionAttribute();

        attribute.FailureMode.ShouldBe(AttestationFailureMode.Enforce);
        attribute.Reason.ShouldBeNull();
        attribute.RecordType.ShouldBeNull();
    }

    #endregion
}
