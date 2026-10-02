#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Attributes;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Sanitization;

/// <summary>
/// The sanitization and output-encoding failure logs carry the error code, never the error message (#1557).
/// </summary>
public sealed class SanitizationPipelineBehaviorsRedactionTests : IDisposable
{
    // The message SanitizationErrors.PropertyError builds for property "Title".
    private const string ErrorMessage = "Sanitization failed for property 'Title'.";
    private const string ErrorCode = SanitizationErrors.PropertyErrorCode;

    private static readonly IOptions<SanitizationOptions> TelemetryOptions =
        Options.Create(new SanitizationOptions { EnableTracing = true, EnableMetrics = true });

    private readonly IRequestContext _context = RequestContext.CreateForTest(userId: "test-user");

    public SanitizationPipelineBehaviorsRedactionTests() => SanitizationPropertyCache.ClearCache();

    public void Dispose() => SanitizationPropertyCache.ClearCache();

    private static void AssertWarningHasCodeNotMessage<T>(FakeLogger<T> logger)
    {
        var warnings = logger.Collector.GetSnapshot().Where(r => r.Level == LogLevel.Warning).ToList();
        warnings.ShouldNotBeEmpty();
        warnings.ShouldAllBe(r => !r.Message.Contains(ErrorMessage, StringComparison.Ordinal));
        warnings.ShouldContain(r => r.Message.Contains(ErrorCode, StringComparison.Ordinal));
    }

    [Fact]
    public async Task InputSanitization_Failure_LogsTheErrorCodeNotTheMessage()
    {
        var sanitizer = Substitute.For<ISanitizer>();
        sanitizer.SanitizeHtml(Arg.Any<string>()).Throws(new InvalidOperationException("boom"));
        var orchestrator = new SanitizationOrchestrator(
            sanitizer, Options.Create(new SanitizationOptions()), NullLogger<SanitizationOrchestrator>.Instance);
        var logger = new FakeLogger<InputSanitizationPipelineBehavior<HtmlCommand, Unit>>();
        var sut = new InputSanitizationPipelineBehavior<HtmlCommand, Unit>(
            orchestrator, TelemetryOptions, logger);

        var result = await sut.Handle(new HtmlCommand { Title = "x" }, _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        AssertWarningHasCodeNotMessage(logger);
    }

    [Fact]
    public async Task OutputEncoding_Failure_LogsTheErrorCodeNotTheMessage()
    {
        var encoder = Substitute.For<IOutputEncoder>();
        encoder.EncodeForHtml(Arg.Any<string>()).Throws(new InvalidOperationException("boom"));
        var logger = new FakeLogger<OutputEncodingPipelineBehavior<HtmlQuery, HtmlResponse>>();
        var sut = new OutputEncodingPipelineBehavior<HtmlQuery, HtmlResponse>(
            encoder, TelemetryOptions, logger);

        var result = await sut.Handle(new HtmlQuery(), _context,
            () => ValueTask.FromResult<Either<EncinaError, HtmlResponse>>(Right(new HtmlResponse { Title = "x" })),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        AssertWarningHasCodeNotMessage(logger);
    }

    public sealed class HtmlCommand : ICommand<Unit>
    {
        [SanitizeHtml]
        public string Title { get; set; } = string.Empty;
    }

    public sealed class HtmlQuery : IQuery<HtmlResponse> { }

    public sealed class HtmlResponse
    {
        [EncodeForHtml]
        public string Title { get; set; } = string.Empty;
    }
}
