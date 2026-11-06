using Encina.Diagnostics;
using Encina.Security.PII;
using Encina.Security.PII.Attributes;
using Encina.Security.PII.Internal;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.PII;

/// <summary>
/// A <c>[PII]</c> or <c>[SensitiveData]</c> property that is not a string makes masking fail closed in
/// every context (#3032). Every test runs the real masker with a sentinel value that must never appear
/// in an exception message or a log entry.
/// </summary>
public sealed class PIINonStringFailClosedTests : IDisposable
{
    private static readonly Guid SentinelGuid = Guid.Parse("3032aaaa-bbbb-cccc-dddd-eeeeffff3032");

    public PIINonStringFailClosedTests() => PIIPropertyScanner.ClearCache();

    public void Dispose() => PIIPropertyScanner.ClearCache();

    public sealed class PiiGuidDto
    {
        [PII(PIIType.Custom)]
        public Guid Reference { get; set; } = SentinelGuid;

        public string Visible { get; set; } = "visible";
    }

    public sealed class SensitiveIntDto
    {
        [SensitiveData]
        public int Code { get; set; } = 3032;
    }

    public sealed class PiiNullableDto
    {
        [PII(PIIType.Custom)]
        public Guid? Reference { get; set; }
    }

    public sealed class PiiDefaultDto
    {
        [PII(PIIType.Custom)]
        public Guid Reference { get; set; }
    }

    public sealed class PiiAndLogDto
    {
        [PII(PIIType.Custom)]
        [MaskInLogs]
        public DateTime Moment { get; set; } = new DateTime(2026, 11, 5, 1, 2, 3, DateTimeKind.Utc);
    }

    public sealed class MixedDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "john@example.com";

        [PII(PIIType.Custom)]
        public Guid Reference { get; set; } = SentinelGuid;
    }

    public sealed class PiiCollectionDto
    {
        [PII(PIIType.Name)]
        public List<string> Names { get; set; } = ["alice"];
    }

    [PII(PIIType.Email)]
    public sealed class ClassLevelDto
    {
        public string Mail { get; set; } = "john@example.com";

        public Guid Reference { get; set; } = SentinelGuid;

        public int Count { get; set; } = 7;
    }

    public sealed record GuidResponse : IRequest<PiiGuidDto>;

    private static PIIMasker CreateMasker(PIIOptions? options = null, FakeLogger<PIIMasker>? logger = null) =>
        new(
            Options.Create(options ?? new PIIOptions()),
            logger ?? new FakeLogger<PIIMasker>(),
            new ServiceCollection().BuildServiceProvider());

    private static void AssertSafeFailure(Exception ex, string propertyName, string attribute)
    {
        ex.ShouldBeOfType<InvalidOperationException>();
        ex.Message.ShouldContain($"'{propertyName}'");
        ex.Message.ShouldContain($"[{attribute}]");
        ex.Message.ShouldNotContain("MaskInLogs");
        ex.Message.ShouldNotContain(SentinelGuid.ToString());
    }

    [Fact]
    public void MaskObject_PiiGuid_ThrowsWithoutTheValue()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskObject(new PiiGuidDto()));

        AssertSafeFailure(ex, nameof(PiiGuidDto.Reference), "PII");
        ex.Message.ShouldContain(nameof(PiiGuidDto));
    }

    [Fact]
    public void MaskObject_SensitiveDataInt_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskObject(new SensitiveIntDto()));

        AssertSafeFailure(ex, nameof(SensitiveIntDto.Code), "SensitiveData");
    }

    [Fact]
    public void MaskForAudit_Generic_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskForAudit(new PiiGuidDto()));

        AssertSafeFailure(ex, nameof(PiiGuidDto.Reference), "PII");
    }

    [Fact]
    public void MaskForAudit_Object_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskForAudit((object)new PiiGuidDto()));

        AssertSafeFailure(ex, nameof(PiiGuidDto.Reference), "PII");
    }

    [Fact]
    public void MaskForLog_Throws()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskForLog(new PiiGuidDto()));

        AssertSafeFailure(ex, nameof(PiiGuidDto.Reference), "PII");
    }

    [Fact]
    public void MaskForLog_AlwaysMaskedWithMaskInLogs_NamesTheAlwaysMaskedAttribute()
    {
        var ex = Should.Throw<InvalidOperationException>(() => CreateMasker().MaskForLog(new PiiAndLogDto()));

        AssertSafeFailure(ex, nameof(PiiAndLogDto.Moment), "PII");
    }

    [Fact]
    public void AlwaysMaskedWithMaskInLogs_FailsInResponseAndAuditContextsToo()
    {
        var sut = CreateMasker();

        Should.Throw<InvalidOperationException>(() => sut.MaskObject(new PiiAndLogDto()));
        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit(new PiiAndLogDto()));
        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit((object)new PiiAndLogDto()));
    }

    [Fact]
    public void LogMasked_PiiGuid_WritesThePlaceholderAndNeverTheValue()
    {
        var sut = CreateMasker();
        var logger = new FakeLogger();

        logger.LogWarningMasked(sut, "Processing {@Dto}", new PiiGuidDto());

        var entries = logger.Collector.GetSnapshot();
        entries.ShouldContain(r => r.Message.Contains("[MASKING FAILED]"));
        entries.ShouldNotContain(r => r.Message.Contains(SentinelGuid.ToString()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void NullOrDefaultValue_StillFailsClosed(int kind)
    {
        var sut = CreateMasker();
        object dto = kind == 0 ? new PiiNullableDto() : new PiiDefaultDto();

        Should.Throw<InvalidOperationException>(() => sut.MaskObject(dto));
        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit(dto));
        Should.Throw<InvalidOperationException>(() => sut.MaskForLog(dto));
    }

    [Fact]
    public void MixedStringAndUnmaskable_ThrowsAndReturnsNoPartialOutput()
    {
        var sut = CreateMasker();

        Should.Throw<InvalidOperationException>(() => sut.MaskObject(new MixedDto()));
        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit(new MixedDto()));
    }

    [Fact]
    public void CollectionDecoratedWithPii_FailsClosed()
    {
        Should.Throw<InvalidOperationException>(() => CreateMasker().MaskObject(new PiiCollectionDto()));
    }

    [Fact]
    public void MaskInAuditTrailsFalse_AuditContextPassesThrough_ResponseStillFails()
    {
        var sut = CreateMasker(new PIIOptions { MaskInAuditTrails = false });
        var dto = new PiiGuidDto();

        sut.MaskForAudit(dto).ShouldBeSameAs(dto);
        ((PiiGuidDto)sut.MaskForAudit((object)dto)).ShouldBeSameAs(dto);
        Should.Throw<InvalidOperationException>(() => sut.MaskObject(dto));
    }

    [Fact]
    public void MaskInLogsFalse_LogContextPassesThrough()
    {
        var sut = CreateMasker(new PIIOptions { MaskInLogs = false });
        var dto = new PiiGuidDto();

        sut.MaskForLog(dto).ShouldBeSameAs(dto);
    }

    [Fact]
    public void ClassLevelAttribute_NonStringMembersAreUntouched()
    {
        var sut = CreateMasker();

        var result = sut.MaskObject(new ClassLevelDto());

        result.Mail.ShouldNotBe("john@example.com");
        result.Reference.ShouldBe(SentinelGuid);
        result.Count.ShouldBe(7);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pipeline_PiiGuidResponse_ReturnsMaskingFailedError(bool maskInResponses)
    {
        var options = new PIIOptions { MaskInResponses = maskInResponses };
        var sut = new PIIMaskingPipelineBehavior<GuidResponse, PiiGuidDto>(
            CreateMasker(options),
            Options.Create(options),
            new FakeLogger<PIIMaskingPipelineBehavior<GuidResponse, PiiGuidDto>>());
        var response = new PiiGuidDto();

        var result = await sut.Handle(
            new GuidResponse(),
            TestRequestContext.For(TestIdentity.User("user-1")),
            () => ValueTask.FromResult<Either<EncinaError, PiiGuidDto>>(response),
            CancellationToken.None);

        if (maskInResponses)
        {
            result.IsLeft.ShouldBeTrue();
            result.IfLeft(e =>
            {
                e.GetCode().IfNone(string.Empty).ShouldBe(PIIErrors.MaskingFailedCode);
                e.Message.ShouldNotContain(SentinelGuid.ToString());
            });
        }
        else
        {
            result.IsRight.ShouldBeTrue();
        }
    }
}
