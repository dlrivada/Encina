using System.Text.Json;
using System.Text.Json.Serialization;
using Encina.Diagnostics;
using Encina.Security.Audit;
using Encina.Security.PII;
using Encina.Security.PII.Attributes;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.PII;

/// <summary>
/// A type whose serialized root is not a JSON object (a collection-derived type, a type with a scalar
/// <see cref="JsonConverter"/>) and that has decorated members applying to the masking context makes
/// masking fail closed in every context (#3332), instead of returning the object unmasked. Real masking
/// of such roots is #1834. Every test runs the real masker with sentinel values that must never appear
/// in an exception message or a log entry.
/// </summary>
public sealed class PIINonObjectRootFailClosedTests
{
    private const string SentinelEmail = "sentinel.owner@example.com";
    private const int SentinelCode = 731_177;

    /// <summary>Serializes as a JSON array: the decorated members are not in the payload at all.</summary>
    public sealed class TaggedList : List<string>
    {
        [PII(PIIType.Custom)]
        public int Code { get; set; } = SentinelCode;

        [PII(PIIType.Email)]
        public string? Owner { get; set; } = SentinelEmail;
    }

    /// <summary>A collection-derived type with a decorated string member that holds no value.</summary>
    public sealed class EmptyOwnerList : List<string>
    {
        [SensitiveData]
        public string? Owner { get; set; }
    }

    /// <summary>A collection-derived type whose only decorated member is log-only.</summary>
    public sealed class LogOnlyList : List<string>
    {
        [MaskInLogs]
        public string? Note { get; set; } = SentinelEmail;
    }

    /// <summary>A collection-derived type whose only decorated member is a non-string log-only member.</summary>
    public sealed class UnmaskableLogOnlyList : List<string>
    {
        [MaskInLogs]
        public int Code { get; set; } = SentinelCode;
    }

    /// <summary>A collection-derived type with no decorated member.</summary>
    public sealed class PlainList : List<string>;

    /// <summary>Serializes as a JSON string that carries the decorated value in clear.</summary>
    [JsonConverter(typeof(ScalarEmailConverter))]
    public sealed class ScalarEmail
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = SentinelEmail;
    }

    public sealed class ScalarEmailConverter : JsonConverter<ScalarEmail>
    {
        public override ScalarEmail Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new() { Email = reader.GetString() ?? string.Empty };

        public override void Write(Utf8JsonWriter writer, ScalarEmail value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Email);
    }

    /// <summary>Serializes as a JSON <c>null</c> root.</summary>
    [JsonConverter(typeof(NullEmailConverter))]
    public sealed class NullEmail
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = SentinelEmail;
    }

    public sealed class NullEmailConverter : JsonConverter<NullEmail>
    {
        public override bool HandleNull => true;

        public override NullEmail Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new();

        public override void Write(Utf8JsonWriter writer, NullEmail value, JsonSerializerOptions options) =>
            writer.WriteNullValue();
    }

    public sealed record TaggedListQuery : IRequest<TaggedList>;

    [Auditable]
    public sealed class AuditedTaggedList : List<string>
    {
        [PII(PIIType.Email)]
        public string? Owner { get; set; } = SentinelEmail;
    }

    private static PIIMasker CreateMasker(PIIOptions? options = null) =>
        new(
            Options.Create(options ?? new PIIOptions()),
            new FakeLogger<PIIMasker>(),
            new ServiceCollection().BuildServiceProvider());

    private static void AssertSafeFailure(Exception ex, string typeName, params string[] memberNames)
    {
        ex.ShouldBeOfType<InvalidOperationException>();
        ex.Message.ShouldContain($"'{typeName}'");
        foreach (var member in memberNames)
        {
            ex.Message.ShouldContain(member);
        }

        ex.Message.ShouldNotContain(SentinelEmail);
        ex.Message.ShouldNotContain(SentinelCode.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void MaskObject_ListDerivedWithDecoratedMembers_ThrowsWithoutValues()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskObject(new TaggedList { "a" }));

        AssertSafeFailure(ex, nameof(TaggedList), nameof(TaggedList.Code), nameof(TaggedList.Owner));
        ex.Message.ShouldContain("Array");
    }

    [Fact]
    public void MaskForAudit_Generic_ListDerivedWithDecoratedMembers_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskForAudit(new TaggedList()));

        AssertSafeFailure(ex, nameof(TaggedList), nameof(TaggedList.Code), nameof(TaggedList.Owner));
    }

    [Fact]
    public void MaskForAudit_Object_ListDerivedWithDecoratedMembers_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskForAudit((object)new TaggedList()));

        AssertSafeFailure(ex, nameof(TaggedList), nameof(TaggedList.Code), nameof(TaggedList.Owner));
    }

    [Fact]
    public void MaskForLog_ListDerivedWithDecoratedMembers_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskForLog(new TaggedList()));

        AssertSafeFailure(ex, nameof(TaggedList), nameof(TaggedList.Code), nameof(TaggedList.Owner));
    }

    [Fact]
    public void ListDerivedWithEmptyDecoratedMember_StillFailsClosedInEveryContext()
    {
        var sut = CreateMasker();
        var dto = new EmptyOwnerList();

        Should.Throw<InvalidOperationException>(() => sut.MaskObject(dto));
        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit(dto));
        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit((object)dto));
        Should.Throw<InvalidOperationException>(() => sut.MaskForLog(dto));
    }

    [Fact]
    public void ScalarConverter_ThrowsInEveryContextWithoutTheValue()
    {
        var sut = CreateMasker();
        var dto = new ScalarEmail();

        AssertSafeFailure(Should.Throw<InvalidOperationException>(() => sut.MaskObject(dto)), nameof(ScalarEmail), nameof(ScalarEmail.Email));
        AssertSafeFailure(Should.Throw<InvalidOperationException>(() => sut.MaskForAudit(dto)), nameof(ScalarEmail), nameof(ScalarEmail.Email));
        AssertSafeFailure(Should.Throw<InvalidOperationException>(() => sut.MaskForAudit((object)dto)), nameof(ScalarEmail), nameof(ScalarEmail.Email));
        AssertSafeFailure(Should.Throw<InvalidOperationException>(() => sut.MaskForLog(dto)), nameof(ScalarEmail), nameof(ScalarEmail.Email));
    }

    [Fact]
    public void ScalarConverter_MessageNamesTheRootKind()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskObject(new ScalarEmail()));

        ex.Message.ShouldContain("String");
    }

    [Fact]
    public void NullRootConverter_ThrowsInEveryContext()
    {
        var sut = CreateMasker();
        var dto = new NullEmail();

        var ex = Should.Throw<InvalidOperationException>(() => sut.MaskObject(dto));
        AssertSafeFailure(ex, nameof(NullEmail), nameof(NullEmail.Email));
        ex.Message.ShouldContain("Null");
        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit(dto));
        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit((object)dto));
        Should.Throw<InvalidOperationException>(() => sut.MaskForLog(dto));
    }

    [Fact]
    public void LogOnlyMember_ResponseAndAuditReturnUnchanged_LogThrows()
    {
        var sut = CreateMasker();
        var dto = new LogOnlyList { "a" };

        sut.MaskObject(dto).ShouldBeSameAs(dto);
        sut.MaskForAudit(dto).ShouldBeSameAs(dto);
        sut.MaskForAudit((object)dto).ShouldBeSameAs(dto);
        var ex = Should.Throw<InvalidOperationException>(() => sut.MaskForLog(dto));
        AssertSafeFailure(ex, nameof(LogOnlyList), nameof(LogOnlyList.Note));
    }

    [Fact]
    public void UnmaskableLogOnlyMember_ResponseAndAuditReturnUnchanged_LogThrows()
    {
        var sut = CreateMasker();
        var dto = new UnmaskableLogOnlyList { "a" };

        sut.MaskObject(dto).ShouldBeSameAs(dto);
        sut.MaskForAudit(dto).ShouldBeSameAs(dto);
        sut.MaskForAudit((object)dto).ShouldBeSameAs(dto);
        var ex = Should.Throw<InvalidOperationException>(() => sut.MaskForLog(dto));
        AssertSafeFailure(ex, nameof(UnmaskableLogOnlyList), nameof(UnmaskableLogOnlyList.Code));
    }

    [Fact]
    public void UndecoratedListDerivedType_IsReturnedUnchangedInEveryContext()
    {
        var sut = CreateMasker(new PIIOptions().AddSensitiveFieldPattern("owner"));
        var dto = new PlainList { "a" };

        sut.MaskObject(dto).ShouldBeSameAs(dto);
        sut.MaskForAudit(dto).ShouldBeSameAs(dto);
        sut.MaskForAudit((object)dto).ShouldBeSameAs(dto);
        sut.MaskForLog(dto).ShouldBeSameAs(dto);
    }

    [Fact]
    public void PlainList_IsReturnedUnchanged()
    {
        var sut = CreateMasker();
        var list = new List<string> { "a" };

        sut.MaskObject(list).ShouldBeSameAs(list);
        sut.MaskForAudit((object)list).ShouldBeSameAs(list);
        sut.MaskForLog(list).ShouldBeSameAs(list);
    }

    [Fact]
    public void ContextFlagsOff_NothingThrows()
    {
        var sut = CreateMasker(new PIIOptions { MaskInAuditTrails = false, MaskInLogs = false });
        var dto = new TaggedList();
        var scalar = new ScalarEmail();

        sut.MaskForAudit(dto).ShouldBeSameAs(dto);
        sut.MaskForAudit((object)dto).ShouldBeSameAs(dto);
        sut.MaskForLog(dto).ShouldBeSameAs(dto);
        sut.MaskForAudit(scalar).ShouldBeSameAs(scalar);
        sut.MaskForAudit((object)scalar).ShouldBeSameAs(scalar);
        sut.MaskForLog(scalar).ShouldBeSameAs(scalar);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pipeline_ListDerivedResponse_ReturnsMaskingFailedOnlyWhenResponsesAreMasked(bool maskInResponses)
    {
        var options = new PIIOptions { MaskInResponses = maskInResponses };
        var sut = new PIIMaskingPipelineBehavior<TaggedListQuery, TaggedList>(
            CreateMasker(options),
            Options.Create(options),
            new FakeLogger<PIIMaskingPipelineBehavior<TaggedListQuery, TaggedList>>());
        var response = new TaggedList { "a" };

        var result = await sut.Handle(
            new TaggedListQuery(),
            TestRequestContext.For(TestIdentity.User("user-1")),
            () => ValueTask.FromResult<Either<EncinaError, TaggedList>>(response),
            CancellationToken.None);

        if (maskInResponses)
        {
            result.IsLeft.ShouldBeTrue();
            result.IfLeft(e =>
            {
                e.GetCode().IfNone(string.Empty).ShouldBe(PIIErrors.MaskingFailedCode);
                e.Message.ShouldNotContain(SentinelEmail);
            });
        }
        else
        {
            result.IsRight.ShouldBeTrue();
            result.IfRight(r => r.ShouldBeSameAs(response));
        }
    }

    [Fact]
    public void AuditEntryFactory_ListDerivedPayload_ReturnsAuditMaskingFailed()
    {
        var factory = new DefaultOperationAuditEntryFactory(CreateMasker(), Options.Create(new OperationAuditOptions()));

        var result = factory.Create(new AuditedTaggedList(), RequestContext.CreateForTest(), AuditOutcome.Success, null);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e =>
        {
            e.GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.MaskingFailedCode);
            e.Message.ShouldNotContain(SentinelEmail);
        });
    }

    [Fact]
    public void LogMasked_ScalarConverter_WritesThePlaceholderAndNeverTheValue()
    {
        var logger = new FakeLogger();

        logger.LogWarningMasked(CreateMasker(), "Processing {@Dto}", new ScalarEmail());

        var entries = logger.Collector.GetSnapshot();
        entries.ShouldContain(r => r.Message.Contains("[MASKING FAILED]"));
        entries.ShouldNotContain(r => r.Message.Contains(SentinelEmail));
    }
}
