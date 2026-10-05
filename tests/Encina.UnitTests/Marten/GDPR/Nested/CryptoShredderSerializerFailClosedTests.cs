#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;

using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Npgsql;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Marten.GDPR.Nested;

/// <summary>
/// Fail-closed write and read paths at every depth (#1646, #1698): nothing unencrypted is written, a failed write
/// leaves the caller's buffer empty, and only a forgotten subject reads as the placeholder.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CryptoShredderSerializerFailClosedTests : IDisposable
{
    private const string Subject = "subject-7b1d";
    private const string Secret = "patient@example.com";
    private const string Sentinel = "SENTINEL-KEY-STORE-DOWN-31c";

    private readonly ISubjectKeyProvider _keys = Substitute.For<ISubjectKeyProvider>();
    private readonly CryptoHarness _harness;

    public CryptoShredderSerializerFailClosedTests() => _harness = new CryptoHarness(_keys);

    public void Dispose() => _harness.Dispose();

    private static NestedEvent Nested(string? subject, string? email = Secret) => new() { Contact = new ContactInfo { SubjectId = subject, Email = email } };

    private static CollectionsEvent InList(string? subject) => new() { List = [new ContactInfo { SubjectId = subject, Email = Secret }] };

    private void KeyIs(byte[] material, int version = 1) =>
        _keys.GetOrCreateSubjectKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionKey>(new SubjectEncryptionKey { Version = version, KeyMaterial = material }));

    // -- Write ---------------------------------------------------------------------------------------------------

    public static TheoryData<object> MissingSubjectAtDepth => new()
    {
        new TopLevelOwner { PatientId = null, Email = Secret },
        Nested(null),
        Nested("   "),
        InList(""),
        new SubjectTypedOwner { PatientId = Guid.Empty, Email = Secret },
    };

    [Theory]
    [MemberData(nameof(MissingSubjectAtDepth))]
    public void Write_MissingSubjectAtAnyDepth_ThrowsWithoutAKeyLookupOrPlaintext(object value)
    {
        var ex = Should.Throw<CryptoShreddingEncryptionException>(() => _harness.Serializer.ToJson(value));

        ex.Reason.ShouldBe(CryptoShreddingEncryptionFailureReason.SubjectIdMissing);
        ex.DocumentTypeName.ShouldBe(value.GetType().FullName);
        ex.Message.ShouldNotContain(Secret);
        _keys.ReceivedCalls().ShouldBeEmpty();
        _harness.Logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Error && r.Id.Id == 8466);
    }

    [Fact]
    public void Write_ObjectTypedSubjectId_IsRejectedAsUnsupportedBeforeAnyValue()
    {
        var ex = Should.Throw<CryptoShreddingConfigurationException>(
            () => _harness.Serializer.ToJson(new ObjectSubjectOwner { PatientId = new object(), Email = Secret }));

        ex.Issues.ShouldContain(i => i.Problems.HasFlag(CryptoShreddedPropertyProblems.SubjectIdTypeUnsupported));
    }

    [Fact]
    public void Engine_UnsupportedRuntimeSubjectValue_ThrowsSubjectIdInvalidOnWriteAndRead()
    {
        // The classifier rejects an unsupported declared type; the engine still maps an unsupported runtime value
        // to SubjectIdInvalid instead of a raw InvalidOperationException (defence in depth).
        var field = new CryptoShreddedField(
            typeof(ObjectSubjectOwner).GetProperty(nameof(ObjectSubjectOwner.Email))!,
            typeof(ObjectSubjectOwner).GetProperty(nameof(ObjectSubjectOwner.PatientId))!,
            new global::Encina.Compliance.DataSubjectRights.PersonalDataAttribute());
        var engine = new CryptoShreddingEngine(_harness.ScopeFactory, _harness.Logger, CryptoHarness.Placeholder);
        var owner = new ObjectSubjectOwner { PatientId = new object(), Email = "cs2:1:AAAA" };

        Should.Throw<CryptoShreddingEncryptionException>(() => engine.EncryptForWrite(owner, field, Secret))
            .Reason.ShouldBe(CryptoShreddingEncryptionFailureReason.SubjectIdInvalid);
        Should.Throw<CryptoShreddingDecryptionException>(
                () => engine.OnOwnerDeserialized(owner, new CryptoShreddingTypePlan(typeof(ObjectSubjectOwner), [field])))
            .Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.SubjectIdInvalid);
    }

    [Fact]
    public void Write_KeyProviderLeftAtDepth_ThrowsKeyUnavailableWithTheCodeOnly()
    {
        _keys.GetOrCreateSubjectKeyAsync(Subject, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SubjectEncryptionKey>(CryptoShreddingErrors.KeyStoreError("GetOrCreateSubjectKey")));

        var ex = Should.Throw<CryptoShreddingEncryptionException>(() => _harness.Serializer.ToJson(InList(Subject)));

        ex.Reason.ShouldBe(CryptoShreddingEncryptionFailureReason.KeyUnavailable);
        ex.ErrorCode.ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);
        ex.DeclaringTypeName.ShouldBe(typeof(ContactInfo).FullName);
        ex.PropertyName.ShouldBe(nameof(ContactInfo.Email));
        ex.Message.ShouldNotContain(Subject);
        _harness.Logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8455);
    }

    [Fact]
    public void Write_KeyProviderThrows_ThrowsKeyUnavailableAndNeverLogsTheExceptionMessage()
    {
        _keys.GetOrCreateSubjectKeyAsync(Subject, Arg.Any<CancellationToken>()).Throws(new InvalidOperationException(Sentinel));

        var ex = Should.Throw<CryptoShreddingEncryptionException>(() => _harness.Serializer.ToJson(Nested(Subject)));

        ex.ErrorCode.ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);
        ex.InnerException.ShouldBeNull();
        ex.Message.ShouldNotContain(Sentinel);
        foreach (var record in _harness.Logger.Collector.GetSnapshot())
        {
            record.Message.ShouldNotContain(Sentinel);
            (record.Exception?.Message ?? string.Empty).ShouldNotContain(Sentinel);
        }
    }

    [Theory]
    [InlineData(16, 1)]
    [InlineData(24, 1)]
    [InlineData(31, 1)]
    [InlineData(32, 0)]
    public void Write_UnusableKey_ThrowsKeyUnavailable(int length, int version)
    {
        KeyIs(new byte[length], version);

        var ex = Should.Throw<CryptoShreddingEncryptionException>(() => _harness.Serializer.ToJson(Nested(Subject)));

        ex.Reason.ShouldBe(CryptoShreddingEncryptionFailureReason.KeyUnavailable);
        ex.ErrorCode.ShouldBe(CryptoShreddingErrors.EncryptionFailedCode);
    }

    [Fact]
    public void Write_ForgottenSubjectWithNewData_ThrowsSubjectForgotten()
    {
        _keys.GetOrCreateSubjectKeyAsync(Subject, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SubjectEncryptionKey>(CryptoShreddingErrors.SubjectForgotten(Subject)));

        var ex = Should.Throw<CryptoShreddingEncryptionException>(() => _harness.Serializer.ToJson(Nested(Subject)));

        ex.Reason.ShouldBe(CryptoShreddingEncryptionFailureReason.KeyUnavailable);
        ex.ErrorCode.ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
    }

    [Fact]
    public void Write_ForgottenSubjectWithThePlaceholder_WritesTheTombstone()
    {
        _keys.GetOrCreateSubjectKeyAsync(Subject, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SubjectEncryptionKey>(CryptoShreddingErrors.SubjectForgotten(Subject)));

        var json = _harness.Serializer.ToJson(Nested(Subject, CryptoHarness.Placeholder));

        json.ShouldContain(CryptoShreddingToken.Tombstone);
        _harness.Logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8474);
    }

    [Fact]
    public void Write_OneKeyLookupPerSubjectPerCall()
    {
        KeyIs(new byte[32]);

        _harness.Serializer.ToJson(new CollectionsEvent { List = [new() { SubjectId = Subject, Email = "a" }, new() { SubjectId = Subject, Email = "b" }] });

        _keys.Received(1).GetOrCreateSubjectKeyAsync(Subject, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WriteTo_Failure_LeavesTheCallersBufferEmpty()
    {
        var buffer = new ArrayBufferWriter<byte>();

        Should.Throw<CryptoShreddingEncryptionException>(() => _harness.Serializer.WriteTo(buffer, Nested(null)));
        Should.Throw<CryptoShreddingEncryptionException>(() => _harness.Serializer.WriteToCleanJson(buffer, Nested(null)));
        Should.Throw<CryptoShreddingEncryptionException>(() => _harness.Serializer.WriteToJsonWithTypes(buffer, Nested(null)));

        buffer.WrittenCount.ShouldBe(0);
    }

    [Fact]
    public void WriteToParameter_Failure_LeavesTheValueNull()
    {
        var parameter = new NpgsqlParameter();

        Should.Throw<CryptoShreddingEncryptionException>(() => _harness.Serializer.WriteToParameter(parameter, Nested(null)));

        parameter.Value.ShouldBeNull();
    }

    [Fact]
    public void Write_MisconfiguredNestedType_ThrowsConfigurationBeforeAnyByte()
    {
        var buffer = new ArrayBufferWriter<byte>();

        var ex = Should.Throw<CryptoShreddingConfigurationException>(
            () => _harness.Serializer.WriteTo(buffer, new ObjectMemberEvent { Payload = new GetterOnlyOwner(Subject, Secret) }));

        ex.Problem.ShouldBe(CryptoShreddingConfigurationProblem.MisconfiguredProperties);
        ex.Issues.ShouldContain(i => i.Problems.HasFlag(CryptoShreddedPropertyProblems.NoSetter));
        buffer.WrittenCount.ShouldBe(0);
        _harness.Logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8459);
    }

    // -- Read ----------------------------------------------------------------------------------------------------

    private static (CryptoHarness Writer, string Json) Written(object value)
    {
        var writer = new CryptoHarness();
        return (writer, writer.Serializer.ToJson(value));
    }

    private static string ReplaceNestedEmail(string json, string replacement)
    {
        var node = JsonNode.Parse(json)!;
        node["Contact"]!["Email"] = replacement;
        return node.ToJsonString();
    }

    [Fact]
    public async Task Read_ForgottenSubjectAtDepth_ReadsThePlaceholderAndNotifiesOncePerSubject()
    {
        var (writer, json) = Written(new CollectionsEvent { List = [new() { SubjectId = Subject, Email = "a" }, new() { SubjectId = Subject, Email = "b" }] });
        using var _ = writer;
        _keys.GetSubjectKeyAsync(Subject, Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, byte[]>(CryptoShreddingErrors.SubjectForgotten(Subject)));

        var sync = _harness.FromJson<CollectionsEvent>(json);
        var asynchronous = await _harness.FromJsonAsync<CollectionsEvent>(json);

        sync.List.ShouldAllBe(c => c!.Email == CryptoHarness.Placeholder);
        asynchronous.List.ShouldAllBe(c => c!.Email == CryptoHarness.Placeholder);
        await _harness.Handler.Received(2).HandleForgottenSubjectAsync(Subject, "List[].Email", typeof(CollectionsEvent), Arg.Any<CancellationToken>());
        _harness.Logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8454);
    }

    [Fact]
    public void Read_ForgottenSubjectHandlerThrows_IsLoggedAndSwallowed()
    {
        var (writer, json) = Written(Nested(Subject));
        using var _ = writer;
        _keys.GetSubjectKeyAsync(Subject, Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, byte[]>(CryptoShreddingErrors.SubjectForgotten(Subject)));
        _harness.Handler.HandleForgottenSubjectAsync(default!, default!, default!, default).ReturnsForAnyArgs(_ => throw new InvalidOperationException(Sentinel));

        _harness.FromJson<NestedEvent>(json).Contact!.Email.ShouldBe(CryptoHarness.Placeholder);

        var record = _harness.Logger.Collector.GetSnapshot().Single(r => r.Id.Id == 8475);
        (record.Exception?.Message ?? string.Empty).ShouldNotContain(Sentinel);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, CryptoShreddingDecryptionFailureReason.IntegrityCheckFailed)]
    public async Task Read_Tombstone_IsThePlaceholderOnlyForAConfirmedForgottenSubject(bool forgotten, CryptoShreddingDecryptionFailureReason? failure)
    {
        var json = ReplaceNestedEmail(JsonSerializer.Serialize(Nested(Subject, null)), CryptoShreddingToken.Tombstone);
        _keys.IsSubjectForgottenAsync(Subject, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(forgotten));

        if (failure is null)
        {
            _harness.FromJson<NestedEvent>(json).Contact!.Email.ShouldBe(CryptoHarness.Placeholder);
            (await _harness.FromJsonAsync<NestedEvent>(json)).Contact!.Email.ShouldBe(CryptoHarness.Placeholder);
            return;
        }

        Should.Throw<CryptoShreddingDecryptionException>(() => _harness.FromJson<NestedEvent>(json)).Reason.ShouldBe(failure.Value);
        (await Should.ThrowAsync<CryptoShreddingDecryptionException>(() => _harness.FromJsonAsync<NestedEvent>(json))).Reason.ShouldBe(failure.Value);
    }

    [Fact]
    public void Read_TombstoneWithAProviderError_ThrowsKeyUnavailable_OneCheckPerSubjectPerCall()
    {
        var json = JsonSerializer.Serialize(new CollectionsEvent
        {
            List = [new() { SubjectId = Subject, Email = CryptoShreddingToken.Tombstone }, new() { SubjectId = Subject, Email = CryptoShreddingToken.Tombstone }],
        });
        _keys.IsSubjectForgottenAsync(Subject, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));

        _harness.FromJson<CollectionsEvent>(json).List.ShouldAllBe(c => c!.Email == CryptoHarness.Placeholder);
        _keys.Received(1).IsSubjectForgottenAsync(Subject, Arg.Any<CancellationToken>());

        _keys.IsSubjectForgottenAsync(Subject, Arg.Any<CancellationToken>()).Throws(new InvalidOperationException(Sentinel));
        Should.Throw<CryptoShreddingDecryptionException>(() => _harness.FromJson<CollectionsEvent>(json))
            .Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.KeyUnavailable);
    }

    [Fact]
    public void Read_TombstoneWithMissingSubject_ThrowsSubjectIdMissingFirst()
    {
        var json = JsonSerializer.Serialize(Nested(null, CryptoShreddingToken.Tombstone));

        Should.Throw<CryptoShreddingDecryptionException>(() => _harness.FromJson<NestedEvent>(json))
            .Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.SubjectIdMissing);
        _keys.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void Read_MalformedValue_ThrowsEnvelopeMalformed()
    {
        var json = JsonSerializer.Serialize(Nested(Subject, "plain-not-a-token"));

        var ex = Should.Throw<CryptoShreddingDecryptionException>(() => _harness.FromJson<NestedEvent>(json));

        ex.Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.EnvelopeMalformed);
        ex.ErrorCode.ShouldBe(CryptoShreddingErrors.EnvelopeMalformedCode);
    }

    [Fact]
    public async Task Read_TokenCopiedFromAnotherSubject_FailsTheIntegrityCheck()
    {
        var real = new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
        using var harness = new CryptoHarness(real);
        var json = harness.Serializer.ToJson(Nested(Subject));
        await real.GetOrCreateSubjectKeyAsync("other");
        var swapped = JsonNode.Parse(json)!;
        swapped["Contact"]!["SubjectId"] = "other";

        var ex = Should.Throw<CryptoShreddingDecryptionException>(() => harness.FromJson<NestedEvent>(swapped.ToJsonString()));

        ex.Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.IntegrityCheckFailed);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(31)]
    public void Read_UnusableKey_ThrowsKeyUnavailableNotIntegrity(int length)
    {
        var (writer, json) = Written(Nested(Subject));
        using var _ = writer;
        _keys.GetSubjectKeyAsync(Subject, 1, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, byte[]>(new byte[length]));

        Should.Throw<CryptoShreddingDecryptionException>(() => _harness.FromJson<NestedEvent>(json))
            .Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.KeyUnavailable);
    }

    [Fact]
    public async Task Read_TransientLeftAndExceptions_ThrowKeyUnavailable()
    {
        var (writer, json) = Written(Nested(Subject));
        using var _ = writer;
        _keys.GetSubjectKeyAsync(Subject, 1, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, byte[]>(CryptoShreddingErrors.KeyStoreError("GetSubjectKey")));

        Should.Throw<CryptoShreddingDecryptionException>(() => _harness.FromJson<NestedEvent>(json)).ErrorCode.ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);

        _keys.GetSubjectKeyAsync(Subject, 1, Arg.Any<CancellationToken>()).Throws(new InvalidOperationException(Sentinel));
        var ex = await Should.ThrowAsync<CryptoShreddingDecryptionException>(() => _harness.FromJsonAsync<NestedEvent>(json));
        ex.Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.KeyUnavailable);
        ex.Message.ShouldNotContain(Sentinel);
        _harness.Logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8456);
    }

    [Fact]
    public async Task ReadAsync_PassesTheCancellationTokenAndFetchesOncePerSubjectAndVersion()
    {
        var real = new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
        using var writer = new CryptoHarness(real);
        var json = writer.Serializer.ToJson(new CollectionsEvent { List = [new() { SubjectId = Subject, Email = "a" }, new() { SubjectId = Subject, Email = "b" }] });
        var material = (await real.GetSubjectKeyAsync(Subject, 1)).Match(k => k, _ => []);
        _keys.GetSubjectKeyAsync(Subject, 1, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, byte[]>(material));
        using var cts = new CancellationTokenSource();

        var read = await _harness.FromJsonAsync<CollectionsEvent>(json, cts.Token);

        read.List.Select(c => c!.Email).ShouldBe(["a", "b"]);
        await _keys.Received(1).GetSubjectKeyAsync(Subject, 1, cts.Token);
    }

    [Fact]
    public void Read_SubjectMissing_ThrowsSubjectIdMissing()
    {
        var (writer, json) = Written(Nested(Subject));
        using var _ = writer;
        var node = JsonNode.Parse(json)!;
        node["Contact"]!["SubjectId"] = null;

        Should.Throw<CryptoShreddingDecryptionException>(() => _harness.FromJson<NestedEvent>(node.ToJsonString()))
            .Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.SubjectIdMissing);
        _harness.Logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8466);
    }

    [Fact]
    public async Task Read_KeyRotation_DecryptsBothVersionsInOneCall()
    {
        var real = new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
        using var harness = new CryptoHarness(real);
        var first = harness.Serializer.ToJson(new ContactInfo { SubjectId = Subject, Email = "v1" });
        await real.RotateSubjectKeyAsync(Subject);
        var second = harness.Serializer.ToJson(new ContactInfo { SubjectId = Subject, Email = "v2" });
        var combined = new JsonArray(JsonNode.Parse(first), JsonNode.Parse(second)).ToJsonString();

        var read = harness.FromJson<List<ContactInfo>>(combined);

        read.Select(c => c.Email).ShouldBe(["v1", "v2"]);
        second.ShouldContain("cs2:2:");
    }

    [Fact]
    public async Task Read_InMemoryRestart_NeverReadsAsThePlaceholder()
    {
        var before = new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
        using var writer = new CryptoHarness(before);
        var json = writer.Serializer.ToJson(Nested(Subject));
        var after = new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
        using var reader = new CryptoHarness(after);

        Should.Throw<CryptoShreddingDecryptionException>(() => reader.FromJson<NestedEvent>(json)).Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.KeyUnavailable);

        await after.GetOrCreateSubjectKeyAsync(Subject);
        Should.Throw<CryptoShreddingDecryptionException>(() => reader.FromJson<NestedEvent>(json)).Reason.ShouldBe(CryptoShreddingDecryptionFailureReason.IntegrityCheckFailed);
    }

    [Fact]
    public void Read_PlaintextEqualToThePlaceholder_IsCountedAsDecrypted()
    {
        var real = new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
        using var harness = new CryptoHarness(real);
        var json = harness.Serializer.ToJson(Nested(Subject, CryptoHarness.Placeholder));

        harness.FromJson<NestedEvent>(json).Contact!.Email.ShouldBe(CryptoHarness.Placeholder);

        harness.Handler.ReceivedCalls().ShouldBeEmpty();
        harness.Logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8451);
    }

    [Fact]
    public void Read_SubjectFilter_LeavesOtherSubjectsUntouchedAndFetchesNoneOfTheirKeys()
    {
        var real = new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
        using var writer = new CryptoHarness(real);
        var json = writer.Serializer.ToJson(new TwoSubjectEvent
        {
            PatientId = Subject,
            PatientEmail = Secret,
            Therapist = new ContactInfo { SubjectId = "therapist", Email = "t@example.com" },
        });
        var material = real.GetSubjectKeyAsync(Subject, 1).AsTask().Result.Match(k => k, _ => []);
        _keys.GetSubjectKeyAsync(Subject, 1, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, byte[]>(material));

        TwoSubjectEvent read;
        using (CryptoShreddingCallScope.FilterToSubject(Subject))
        {
            read = _harness.FromJson<TwoSubjectEvent>(json);
        }

        read.PatientEmail.ShouldBe(Secret);
        read.Therapist!.Email!.ShouldStartWith(CryptoShreddingToken.Prefix);
        _keys.DidNotReceive().GetSubjectKeyAsync("therapist", Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ImplicitScope_OptionsUsedOutsideTheSerializer_StillEncryptAndDecrypt()
    {
        var real = new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
        using var harness = new CryptoHarness(real);
        harness.Serializer.ToJson(new NoPiiEvent());
        var options = harness.Serializer.WalkOptions!;

        var json = JsonSerializer.Serialize(Nested(Subject), options);
        var read = JsonSerializer.Deserialize<NestedEvent>(json, options)!;

        json.ShouldNotContain(Secret);
        read.Contact!.Email.ShouldBe(Secret);
        harness.Logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8476);
    }
}
