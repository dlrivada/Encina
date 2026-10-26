using Encina.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Message-exposure rules of <see cref="ProblemDetailsExtensions"/> (#2649): the error message and the
/// exception details reach the client only when the option is on and the host is Development.
/// </summary>
public sealed class ProblemDetailsMessageExposureTests
{
    private const string Secret = "secret-internal-detail-4711";

    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, int EventId, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, eventId.Id, formatter(state, exception)));
    }

    private static DefaultHttpContext CreateContext(bool option, string? environment)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .Configure<EncinaAspNetCoreOptions>(o => o.IncludeErrorMessageInProblemDetails = option);
        if (environment is not null)
        {
            services.AddSingleton<IHostEnvironment>(new FakeEnvironment(environment));
        }

        return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
    }

    private static EncinaError ErrorWithException() =>
        EncinaErrors.FromException("test.exception", new InvalidOperationException(Secret));

    private static ProblemDetails Details(IResult result) =>
        result.ShouldBeOfType<ProblemHttpResult>().ProblemDetails;

    [Fact]
    public void ToProblemDetails_Default_OmitsDetailAndNeverCarriesTheMessage()
    {
        var error = EncinaErrors.Create("validation.field_required", Secret);
        var context = CreateContext(option: false, environment: "Development");
        context.TraceIdentifier = "trace-1";

        var details = Details(error.ToProblemDetails(context));

        details.Detail.ShouldBeNull();
        details.Title.ShouldBe("Bad Request");
        details.Status.ShouldBe(StatusCodes.Status400BadRequest);
        details.Extensions["errorCode"].ShouldBe("validation.field_required");
        details.Extensions["traceId"].ShouldBe("trace-1");
    }

    [Fact]
    public void ToProblemDetails_OptionOnInDevelopment_IncludesTheMessage()
    {
        var error = EncinaErrors.Create("test.error", Secret);

        var details = Details(error.ToProblemDetails(CreateContext(option: true, environment: "Development")));

        details.Detail.ShouldBe(Secret);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData(null)]
    public void ToProblemDetails_OptionOnOutsideDevelopmentOrWithoutEnvironment_OmitsTheMessage(string? environment)
    {
        var error = EncinaErrors.Create("test.error", Secret);

        var details = Details(error.ToProblemDetails(CreateContext(option: true, environment)));

        details.Detail.ShouldBeNull();
    }

    [Fact]
    public void ToProblemDetails_ExceptionExtension_DefaultInDevelopment_IsOmitted()
    {
        var details = Details(ErrorWithException().ToProblemDetails(
            CreateContext(option: false, environment: "Development")));

        details.Extensions.ShouldNotContainKey("exception");
    }

    [Fact]
    public void ToProblemDetails_ExceptionExtension_OptionOnInDevelopment_CarriesTypeMessageAndStack()
    {
        var details = Details(ErrorWithException().ToProblemDetails(
            CreateContext(option: true, environment: "Development")));

        details.Extensions.ShouldContainKey("exception");
        details.Extensions["exception"].ShouldNotBeNull().ToString()!.ShouldContain(Secret);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData(null)]
    public void ToProblemDetails_ExceptionExtension_OptionOnOutsideDevelopment_IsOmitted(string? environment)
    {
        var details = Details(ErrorWithException().ToProblemDetails(CreateContext(option: true, environment)));

        details.Extensions.ShouldNotContainKey("exception");
        details.Detail.ShouldBeNull();
    }

    [Fact]
    public async Task ToActionResult_FollowsTheSameRule()
    {
        var error = EncinaErrors.Create("test.error", Secret);

        var hidden = CreateContext(option: true, environment: "Production");
        hidden.Response.Body = new MemoryStream();
        await error.ToActionResult(hidden).ExecuteResultAsync(new ActionContext { HttpContext = hidden });
        hidden.Response.Body.Position = 0;
        (await new StreamReader(hidden.Response.Body).ReadToEndAsync()).ShouldNotContain(Secret);

        var shown = CreateContext(option: true, environment: "Development");
        shown.Response.Body = new MemoryStream();
        await error.ToActionResult(shown).ExecuteResultAsync(new ActionContext { HttpContext = shown });
        shown.Response.Body.Position = 0;
        (await new StreamReader(shown.Response.Body).ReadToEndAsync()).ShouldContain(Secret);
    }

    // ── Startup warning service ─────────────────────────────────────────────

    private static CapturingLogger<ProblemDetailsStartupWarningService> RunStartup(bool option, string? environment)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<EncinaAspNetCoreOptions>>(
            Options.Create(new EncinaAspNetCoreOptions { IncludeErrorMessageInProblemDetails = option }));
        if (environment is not null)
        {
            services.AddSingleton<IHostEnvironment>(new FakeEnvironment(environment));
        }

        using var provider = services.BuildServiceProvider();
        var logger = new CapturingLogger<ProblemDetailsStartupWarningService>();
        var hosted = new ProblemDetailsStartupWarningService(
            provider.GetRequiredService<IOptions<EncinaAspNetCoreOptions>>(), logger, provider);

        hosted.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        hosted.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        return logger;
    }

    [Fact]
    public void StartupWarning_OptionOffInDevelopment_LogsNothing()
    {
        RunStartup(option: false, environment: "Development").Entries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData(null)]
    public void StartupWarning_OptionOnOutsideDevelopment_LogsTheIgnoredWarningOnce(string? environment)
    {
        var entry = RunStartup(option: true, environment).Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Warning);
        entry.EventId.ShouldBe(205);
    }

    [Fact]
    public void StartupWarning_OptionOnInDevelopment_LogsTheHonouredWarningOnce()
    {
        var entry = RunStartup(option: true, environment: "Development").Entries.ShouldHaveSingleItem();

        entry.Level.ShouldBe(LogLevel.Warning);
        entry.EventId.ShouldBe(206);
    }

    [Fact]
    public void StartupWarning_NullArguments_Throw()
    {
        var options = Options.Create(new EncinaAspNetCoreOptions());
        using var provider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger<ProblemDetailsStartupWarningService>.Instance;

        Should.Throw<ArgumentNullException>(() => new ProblemDetailsStartupWarningService(null!, logger, provider));
        Should.Throw<ArgumentNullException>(() => new ProblemDetailsStartupWarningService(options, null!, provider));
        Should.Throw<ArgumentNullException>(() => new ProblemDetailsStartupWarningService(options, logger, null!));
    }

    [Fact]
    public void AddEncinaAspNetCore_RegistersTheStartupWarningOnce_AndBuildsWithValidation()
    {
        var services = new ServiceCollection();

        services.AddEncinaAspNetCore(o => o.IncludeErrorMessageInProblemDetails = true);
        services.AddEncinaAspNetCore();
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetServices<IHostedService>()
            .OfType<ProblemDetailsStartupWarningService>()
            .ShouldHaveSingleItem();
    }
}
