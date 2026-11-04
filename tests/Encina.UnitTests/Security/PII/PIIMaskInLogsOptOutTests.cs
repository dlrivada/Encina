using Encina.Security.PII;
using Encina.Security.PII.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.PII;

/// <summary>
/// <c>PIIOptions.MaskInLogs = false</c> is an explicit, logged opt-out (#1839, EventId 8024): the options
/// validator warns once per process, names the option and never a value.
/// </summary>
public sealed class PIIMaskInLogsOptOutTests
{
    private static (PIIOptionsValidator Sut, FakeLogger<PIIOptionsValidator> Logger) Create()
    {
        var logger = new FakeLogger<PIIOptionsValidator>();
        return (new PIIOptionsValidator(logger), logger);
    }

    [Fact]
    public void Validate_MaskInLogsDisabled_LogsEventId8024OnceForRepeatedCalls()
    {
        var (sut, logger) = Create();
        var options = new PIIOptions { MaskInLogs = false };

        sut.Validate(null, options).Succeeded.ShouldBeTrue();
        sut.Validate(null, options).Succeeded.ShouldBeTrue();
        sut.Validate("named", options).Succeeded.ShouldBeTrue();

        var entry = logger.Collector.GetSnapshot().Where(r => r.Id.Id == 8024).ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public void Validate_MaskInLogsEnabled_NeverLogsEventId8024()
    {
        var (sut, logger) = Create();

        sut.Validate(null, new PIIOptions()).Succeeded.ShouldBeTrue();
        sut.Validate(null, new PIIOptions { MaskInLogs = true }).Succeeded.ShouldBeTrue();

        logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 8024);
    }

    [Fact]
    public void Validate_MaskInLogsDisabledWithHashKey_MessageNamesTheOptionAndNeverAValue()
    {
        const string key = "0123456789abcdef0123456789abcdef";
        var (sut, logger) = Create();

        sut.Validate(null, new PIIOptions { MaskInLogs = false, HashKey = key });

        var entry = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 8024);
        entry.Message.ShouldContain("PIIOptions.MaskInLogs");
        entry.Message.ShouldNotContain(key);
    }

    [Fact]
    public void Validate_InvalidOptionsWithMaskInLogsDisabled_FailsWithoutWarning()
    {
        var (sut, logger) = Create();

        var result = sut.Validate(null, new PIIOptions { MaskInLogs = false, RegexTimeout = TimeSpan.Zero });

        result.Failed.ShouldBeTrue();
        logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 8024);
    }

    [Fact]
    public void MaskForLog_MaskInLogsDisabled_ReturnsTheSameInstance()
    {
        var sut = new PIIMasker(
            Options.Create(new PIIOptions { MaskInLogs = false }),
            new FakeLogger<PIIMasker>(),
            new ServiceCollection().BuildServiceProvider());
        var dto = new Dto();

        sut.MaskForLog(dto).ShouldBeSameAs(dto);
    }

    private sealed class Dto
    {
        [MaskInLogs]
        public string Id { get; set; } = "secret-1839";
    }
}
