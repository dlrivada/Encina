#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Diagnostics;
using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Attributes;
using Encina.Security.PII.Internal;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.Security.PII;

/// <summary>
/// Failure, tracing and metrics paths of <see cref="PIIMasker"/> and
/// <see cref="PIIMaskingPipelineBehavior{TRequest, TResponse}"/>: failures never expose the
/// exception message to the logger and never turn into a request failure.
/// </summary>
public sealed class PIIMaskingFailureRedactionTests : IDisposable
{
    private const string Sentinel = "sentinel-secret-message-1557";

    public PIIMaskingFailureRedactionTests()
    {
        PIIPropertyScanner.ClearCache();
    }

    public void Dispose()
    {
        PIIPropertyScanner.ClearCache();
    }

    private sealed class ThrowingStrategy : IMaskingStrategy
    {
        public string Apply(string value, MaskingOptions options) => throw new InvalidOperationException(Sentinel);
    }

    private sealed class EmailDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "john@example.com";

        public string Other { get; set; } = "visible";
    }

    private sealed class LogOnlyDto
    {
        [MaskInLogs]
        public string Email { get; set; } = "john@example.com";

        [PII(PIIType.Name)]
        public string? Name { get; set; }
    }

    private sealed record TestRequest : IRequest<EmailDto>;

    private static PIIMasker CreateMasker(PIIOptions options, FakeLogger<PIIMasker> logger)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ThrowingStrategy>();
        return new PIIMasker(Options.Create(options), logger, services.BuildServiceProvider());
    }

    private static void AssertLoggedRedacted<T>(FakeLogger<T> logger)
    {
        var entry = logger.Collector.GetSnapshot().Single(r => r.Exception is not null);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(Sentinel);
        entry.Message.ShouldNotContain(Sentinel);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaskObject_StrategyThrows_RethrowsAndLogsRedactedException(bool diagnostics)
    {
        var options = new PIIOptions { EnableMetrics = diagnostics, EnableTracing = diagnostics };
        options.AddStrategy<ThrowingStrategy>(PIIType.Email);
        var logger = new FakeLogger<PIIMasker>();
        var sut = CreateMasker(options, logger);

        var act = () => sut.MaskObject(new EmailDto());

        act.ShouldThrow<InvalidOperationException>();
        AssertLoggedRedacted(logger);
    }

    [Fact]
    public void MaskObject_WithTracingAndMetrics_MasksDecoratedPropertyAndKeepsOthers()
    {
        var options = new PIIOptions { EnableMetrics = true, EnableTracing = true };
        var sut = CreateMasker(options, new FakeLogger<PIIMasker>());

        var result = sut.MaskObject(new EmailDto());

        result.Email.ShouldNotBe("john@example.com");
        result.Other.ShouldBe("visible");
    }

    [Fact]
    public void MaskObject_LogOnlyAndNullProperties_AreLeftUntouchedOutsideLogContext()
    {
        var sut = CreateMasker(new PIIOptions(), new FakeLogger<PIIMasker>());

        var result = sut.MaskObject(new LogOnlyDto());

        result.Email.ShouldBe("john@example.com");
        result.Name.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_MaskerThrows_ReturnsErrorAndLogsRedactedException(bool metrics)
    {
        var masker = Substitute.For<IPIIMasker>();
        masker.MaskObject(Arg.Any<EmailDto>()).Throws(new InvalidOperationException(Sentinel));
        var options = new PIIOptions { EnableMetrics = metrics };
        var logger = new FakeLogger<PIIMaskingPipelineBehavior<TestRequest, EmailDto>>();
        var sut = new PIIMaskingPipelineBehavior<TestRequest, EmailDto>(masker, Options.Create(options), logger);
        var response = new EmailDto();

        var result = await sut.Handle(
            new TestRequest(),
            RequestContext.CreateForTest(userId: "user-1"),
            () => ValueTask.FromResult<Either<EncinaError, EmailDto>>(response),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e =>
        {
            e.GetCode().IfNone(string.Empty).ShouldBe(PIIErrors.MaskingFailedCode);
            e.Message.ShouldNotContain(Sentinel);
        });
        AssertLoggedRedacted(logger);
    }
}
