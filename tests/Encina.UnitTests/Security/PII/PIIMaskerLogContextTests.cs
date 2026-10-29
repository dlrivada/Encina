using Encina.Diagnostics;
using Encina.Security.PII;
using Encina.Security.PII.Attributes;
using Encina.Security.PII.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.PII;

/// <summary>
/// Log-context masking (#1839): <see cref="PIIMasker.MaskForLog{T}(T)"/> masks <c>[MaskInLogs]</c>,
/// <c>[PII]</c> and <c>[SensitiveData]</c> properties, while <c>MaskObject</c> and <c>MaskForAudit</c>
/// leave <c>[MaskInLogs]</c> untouched. Every test runs the real masker.
/// </summary>
public sealed class PIIMaskerLogContextTests : IDisposable
{
    private const string Sentinel = "secret-tracking-id-1839";

    public PIIMaskerLogContextTests() => PIIPropertyScanner.ClearCache();

    public void Dispose() => PIIPropertyScanner.ClearCache();

    private sealed class PartialDto { [MaskInLogs(MaskingMode.Partial)] public string Id { get; set; } = Sentinel; }

    private sealed class FullDto { [MaskInLogs(MaskingMode.Full)] public string Id { get; set; } = Sentinel; }

    private sealed class RedactDto { [MaskInLogs(MaskingMode.Redact)] public string Id { get; set; } = Sentinel; }

    private sealed class HashDto { [MaskInLogs(MaskingMode.Hash)] public string Id { get; set; } = Sentinel; }

    private sealed class TokenizeDto { [MaskInLogs(MaskingMode.Tokenize)] public string Id { get; set; } = Sentinel; }

    private sealed class PiiTokenizeDto
    {
        [PII(PIIType.Email, Mode = MaskingMode.Tokenize)]
        public string Email { get; set; } = "john@example.com";
    }

    private sealed class MixedDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "john@example.com";

        [SensitiveData]
        public string Note { get; set; } = Sentinel;

        [MaskInLogs]
        public string Tracking { get; set; } = "tracking-1839";

        public string Visible { get; set; } = "visible";
    }

    private sealed class GetOnlyLogOnlyDto
    {
        [MaskInLogs]
        public string Id { get; } = Sentinel;
    }

    private sealed record ProcessOrderCommand(Guid Id, [property: MaskInLogs] string InternalTrackingId);

    private static PIIMasker CreateMasker(PIIOptions? options, FakeLogger<PIIMasker>? logger = null) =>
        new(
            Options.Create(options ?? new PIIOptions()),
            logger ?? new FakeLogger<PIIMasker>(),
            new ServiceCollection().BuildServiceProvider());

    [Fact]
    public void MaskForLog_PartialFullRedact_MaskTheLogOnlyProperty()
    {
        var sut = CreateMasker(null);

        sut.MaskForLog(new PartialDto()).Id.ShouldNotContain(Sentinel);
        sut.MaskForLog(new FullDto()).Id.ShouldNotContain(Sentinel);
        sut.MaskForLog(new RedactDto()).Id.ShouldBe("[REDACTED]");
    }

    [Fact]
    public void MaskForLog_HashWithKey_ReplacesTheValueWithItsHash()
    {
        var sut = CreateMasker(new PIIOptions { HashKey = "0123456789abcdef0123456789abcdef" });

        var result = sut.MaskForLog(new HashDto());

        result.Id.ShouldNotContain(Sentinel);
        result.Id.Length.ShouldBe(64);
    }

    [Fact]
    public void MaskForLog_HashWithoutKey_FallsBackToRedactAndLogs8020()
    {
        var logger = new FakeLogger<PIIMasker>();
        var sut = CreateMasker(new PIIOptions(), logger);

        var result = sut.MaskForLog(new HashDto());

        result.Id.ShouldBe("[REDACTED]");
        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 8020);
    }

    [Fact]
    public void MaskForLog_TokenizeMode_IsTreatedAsRedactAndNeverReturnsTheRawValue()
    {
        var sut = CreateMasker(null);

        sut.MaskForLog(new TokenizeDto()).Id.ShouldBe("[REDACTED]");
        sut.MaskForLog(new PiiTokenizeDto()).Email.ShouldBe("[REDACTED]");
    }

    [Fact]
    public void MaskObject_TokenizeMode_KeepsTheExistingResponseBehaviour()
    {
        var sut = CreateMasker(null);

        sut.MaskObject(new PiiTokenizeDto()).Email.ShouldBe("john@example.com");
    }

    [Fact]
    public void MaskForLog_MixedAttributes_MasksAllThreeKindsAndKeepsPlainProperties()
    {
        var sut = CreateMasker(null);

        var result = sut.MaskForLog(new MixedDto());

        result.Email.ShouldNotBe("john@example.com");
        result.Note.ShouldNotContain(Sentinel);
        result.Tracking.ShouldNotBe("tracking-1839");
        result.Visible.ShouldBe("visible");
    }

    [Fact]
    public void MaskObjectAndMaskForAudit_LeaveLogOnlyPropertiesUntouched()
    {
        var sut = CreateMasker(null);
        var dto = new MixedDto();

        var response = sut.MaskObject(dto);
        var audit = sut.MaskForAudit(dto);
        var auditUntyped = (MixedDto)sut.MaskForAudit((object)dto);

        response.Tracking.ShouldBe("tracking-1839");
        audit.Tracking.ShouldBe("tracking-1839");
        auditUntyped.Tracking.ShouldBe("tracking-1839");
        response.Email.ShouldNotBe("john@example.com");
    }

    [Fact]
    public void MaskForLog_MaskInLogsDisabled_ReturnsTheSameInstanceUnmasked()
    {
        var sut = CreateMasker(new PIIOptions { MaskInLogs = false });
        var dto = new MixedDto();

        var result = sut.MaskForLog(dto);

        result.ShouldBeSameAs(dto);
        result.Tracking.ShouldBe("tracking-1839");
    }

    [Fact]
    public void MaskForLog_GetOnlyDecoratedProperty_FailsClosedAndLogs8023WithoutTheValue()
    {
        var logger = new FakeLogger<PIIMasker>();
        var sut = CreateMasker(null, logger);

        var act = () => sut.MaskForLog(new GetOnlyLogOnlyDto());

        act.ShouldThrow<InvalidOperationException>();
        var entry = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 8023);
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Message.ShouldNotContain(Sentinel);
        entry.Exception!.ToString().ShouldNotContain(Sentinel);
    }

    [Fact]
    public void LogMasked_WithRealMaskerAndFakeLogger_NeverWritesTheLogOnlyValue()
    {
        var sut = CreateMasker(null);
        var logger = new FakeLogger();

        logger.LogInformationMasked(sut, "Processing {@Cmd}", new ProcessOrderCommand(Guid.NewGuid(), Sentinel));

        var entry = logger.Collector.GetSnapshot().Single();
        entry.Message.ShouldNotContain(Sentinel);
        var logged = string.Join("|", entry.StructuredState!.Select(p => p.Value));
        logged.ShouldContain("InternalTrackingId");
        logged.ShouldNotContain(Sentinel);
    }

    [Fact]
    public void LogMasked_WhenRealMaskerFails_WritesThePlaceholderAndNeverTheValue()
    {
        var sut = CreateMasker(null);
        var logger = new FakeLogger();

        logger.LogWarningMasked(sut, "Processing {@Cmd}", new GetOnlyLogOnlyDto());

        var entries = logger.Collector.GetSnapshot();
        entries.ShouldContain(r => r.Message.Contains("[MASKING FAILED]"));
        entries.ShouldNotContain(r => r.Message.Contains(Sentinel));
    }
}
