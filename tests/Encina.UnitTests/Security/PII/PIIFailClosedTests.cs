using System.Text.Json;
using System.Text.Json.Serialization;
using Encina.Diagnostics;
using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Attributes;
using Encina.Security.PII.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.PII;

/// <summary>
/// Masking fails closed (#1793): no failure path of <see cref="PIIMasker"/> returns or logs the
/// value, the pattern text or an exception message.
/// </summary>
public sealed class PIIFailClosedTests : IDisposable
{
    private const string ValueSentinel = "value-sentinel-1793";
    private const string PatternSentinel = "pattern-sentinel-1793";
    private const string MessageSentinel = "message-sentinel-1793";

    public PIIFailClosedTests()
    {
        PIIPropertyScanner.ClearCache();
    }

    public void Dispose()
    {
        PIIPropertyScanner.ClearCache();
    }

    private sealed class ThrowingStrategy : IMaskingStrategy
    {
        public string Apply(string value, MaskingOptions options) => throw new InvalidOperationException(MessageSentinel);
    }

    private sealed class EmailDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "john@example.com";
    }

    private sealed class BadPatternDto
    {
        [PII(PIIType.Custom, Pattern = "[" + PatternSentinel + "(")]
        public string Notes { get; set; } = ValueSentinel;
    }

    private sealed class SlowPatternDto
    {
        [PII(PIIType.Custom, Pattern = "^(a+)+$")]
        public string Notes { get; set; } = new string('a', 40) + "!";
    }

    private sealed class GetOnlyDto
    {
        [PII(PIIType.Email)]
        public string Email { get; } = "john@example.com";
    }

    private sealed class ThrowingConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new JsonException(MessageSentinel);

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);
    }

    private sealed class UnreadableAttributeDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "john@example.com";

        [JsonConverter(typeof(ThrowingConverter))]
        public int Bad { get; set; }
    }

    private sealed class UnreadableSensitiveDto
    {
        public string Password { get; set; } = ValueSentinel;

        [JsonConverter(typeof(ThrowingConverter))]
        public int Bad { get; set; }
    }

    private static PIIMasker CreateMasker(PIIOptions options, FakeLogger<PIIMasker> logger)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ThrowingStrategy>();
        return new PIIMasker(Options.Create(options), logger, services.BuildServiceProvider());
    }

    private static PIIOptions ThrowingEmailOptions()
    {
        var options = new PIIOptions();
        options.AddStrategy<ThrowingStrategy>(PIIType.Email);
        return options;
    }

    private static void AssertNothingSensitiveLogged(FakeLogger<PIIMasker> logger)
    {
        foreach (var record in logger.Collector.GetSnapshot())
        {
            record.Message.ShouldNotContain(ValueSentinel);
            record.Message.ShouldNotContain(PatternSentinel);
            record.Message.ShouldNotContain(MessageSentinel);
            record.Exception?.ToString().ShouldNotContain(MessageSentinel);
            foreach (var pair in record.StructuredState ?? [])
            {
                (pair.Value ?? string.Empty).ShouldNotContain(ValueSentinel);
                (pair.Value ?? string.Empty).ShouldNotContain(PatternSentinel);
            }
        }
    }

    [Fact]
    public void Mask_InvalidPattern_MasksWholeValueAndLogsOnlyThePatternLength()
    {
        var logger = new FakeLogger<PIIMasker>();
        var sut = CreateMasker(new PIIOptions(), logger);
        var pattern = "[" + PatternSentinel + "(";

        var result = sut.Mask(ValueSentinel, pattern);

        result.ShouldBe(new string('*', ValueSentinel.Length));
        var warning = logger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning);
        warning.Id.Id.ShouldBe(8021);
        warning.Message.ShouldContain(pattern.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AssertNothingSensitiveLogged(logger);
    }

    [Fact]
    public void Mask_CatastrophicPatternWithTinyTimeout_MasksWholeValue()
    {
        var logger = new FakeLogger<PIIMasker>();
        var options = new PIIOptions { RegexTimeout = TimeSpan.FromMilliseconds(1) };
        var sut = CreateMasker(options, logger);
        var value = new string('a', 40) + "!";

        var result = sut.Mask(value, "^(a+)+$");

        result.ShouldBe(new string('*', value.Length));
        logger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning).Id.Id.ShouldBe(8021);
    }

    [Fact]
    public void Mask_InvalidTimeout_MasksWholeValueInsteadOfThrowing()
    {
        var sut = CreateMasker(new PIIOptions { RegexTimeout = TimeSpan.Zero }, new FakeLogger<PIIMasker>());

        sut.Mask("12345", @"\d").ShouldBe("*****");
    }

    [Fact]
    public void MaskObject_InvalidAttributePattern_MasksWholeProperty()
    {
        var logger = new FakeLogger<PIIMasker>();
        var sut = CreateMasker(new PIIOptions(), logger);

        var result = sut.MaskObject(new BadPatternDto());

        result.Notes.ShouldBe(new string('*', ValueSentinel.Length));
        AssertNothingSensitiveLogged(logger);
    }

    [Fact]
    public void MaskObject_CatastrophicAttributePatternWithTinyTimeout_MasksWholeProperty()
    {
        var options = new PIIOptions { RegexTimeout = TimeSpan.FromMilliseconds(1) };
        var sut = CreateMasker(options, new FakeLogger<PIIMasker>());

        var result = sut.MaskObject(new SlowPatternDto());

        result.Notes.ShouldBe(new string('*', 41));
    }

    [Fact]
    public void MaskObject_GetOnlyPiiProperty_ThrowsInsteadOfReturningItUnmasked()
    {
        var sut = CreateMasker(new PIIOptions(), new FakeLogger<PIIMasker>());

        Should.Throw<InvalidOperationException>(() => sut.MaskObject(new GetOnlyDto()));
    }

    private class BaseDto
    {
        public string Title { get; set; } = "t";
    }

    private sealed class DerivedDto : BaseDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "john@example.com";
    }

    [Fact]
    public void MaskObject_DerivedInstanceDeclaredAsBase_DoesNotFailOnDerivedOnlyMembers()
    {
        var sut = CreateMasker(new PIIOptions(), new FakeLogger<PIIMasker>());

        var result = sut.MaskObject<BaseDto>(new DerivedDto());

        result.Title.ShouldBe("t");
    }

    [Fact]
    public void MaskForAudit_Object_GetOnlyPiiProperty_Throws()
    {
        var sut = CreateMasker(new PIIOptions(), new FakeLogger<PIIMasker>());

        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit((object)new GetOnlyDto()));
    }

    [Fact]
    public void MaskForAudit_Generic_StrategyThrows_RethrowsAndLogsRedactedException()
    {
        var logger = new FakeLogger<PIIMasker>();
        var sut = CreateMasker(ThrowingEmailOptions(), logger);

        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit(new EmailDto()));

        var warning = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 8022);
        warning.Exception.ShouldBeOfType<RedactedException>();
        AssertNothingSensitiveLogged(logger);
    }

    [Fact]
    public void MaskForAudit_Object_StrategyThrows_RethrowsAndLogsRedactedException()
    {
        var logger = new FakeLogger<PIIMasker>();
        var sut = CreateMasker(ThrowingEmailOptions(), logger);

        Should.Throw<InvalidOperationException>(() => sut.MaskForAudit((object)new EmailDto()));

        logger.Collector.GetSnapshot().Single(r => r.Id.Id == 8022).Exception.ShouldBeOfType<RedactedException>();
        AssertNothingSensitiveLogged(logger);
    }

    [Fact]
    public void MaskObject_PropertyMetadataPathJsonFailure_RethrowsAndLogsSerializationFailed()
    {
        var logger = new FakeLogger<PIIMasker>();
        var sut = CreateMasker(new PIIOptions(), logger);

        Should.Throw<JsonException>(() => sut.MaskObject(new UnreadableAttributeDto()));

        logger.Collector.GetSnapshot().Any(r => r.Id.Id == 8015).ShouldBeTrue();
        AssertNothingSensitiveLogged(logger);
    }

    [Fact]
    public void MaskObject_SensitiveFieldPathJsonFailure_Rethrows()
    {
        var sut = CreateMasker(new PIIOptions(), new FakeLogger<PIIMasker>());

        Should.Throw<JsonException>(() => sut.MaskObject(new UnreadableSensitiveDto()));
    }

    [Fact]
    public void MaskForAudit_Object_JsonFailure_Rethrows()
    {
        var logger = new FakeLogger<PIIMasker>();
        var sut = CreateMasker(new PIIOptions(), logger);

        Should.Throw<JsonException>(() => sut.MaskForAudit((object)new UnreadableAttributeDto()));

        AssertNothingSensitiveLogged(logger);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Validator_InvalidRegexTimeout_Fails(int milliseconds)
    {
        var options = new PIIOptions { RegexTimeout = TimeSpan.FromMilliseconds(milliseconds) };

        var result = new PIIOptionsValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("RegexTimeout");
    }

    [Fact]
    public void Validator_DefaultRegexTimeout_Is100MillisecondsAndValid()
    {
        var options = new PIIOptions();

        options.RegexTimeout.ShouldBe(TimeSpan.FromMilliseconds(100));
        new PIIOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }
}
