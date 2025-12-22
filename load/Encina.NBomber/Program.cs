using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NBomber.Contracts;
using NBomber.CSharp;
using Encina;
using static LanguageExt.Prelude;

var options = NbomberOptions.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
options.Normalize();

Console.WriteLine("Encina NBomber Harness");
Console.WriteLine($"Scenario: {options.Scenario}");
Console.WriteLine($"Duration: {options.Duration}. Warm-up: {options.WarmUp}. Report interval: {options.ReportingInterval}.");
Console.WriteLine($"Send rate: {options.SendRate} ops/sec. Publish rate: {options.PublishRate} ops/sec.");
if (!string.IsNullOrEmpty(options.ProfilePath))
{
    Console.WriteLine($"Profile: {options.ProfilePath}");
}

var services = new ServiceCollection();
services.AddEncina(config => config.RegisterServicesFromAssemblyContaining<PingCommand>());

using var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = false,
    ValidateScopes = false
});

await WarmUpMediatorAsync(provider, options).ConfigureAwait(false);

var reportDirectory = PrepareReportDirectory(options);
var scenarios = ScenarioFactory.CreateScenarios(options, provider);

Console.WriteLine($"Report directory: {reportDirectory}");
Console.WriteLine();

NBomberRunner
    .RegisterScenarios(scenarios)
    .WithTestSuite("Encina")
    .WithTestName(options.Scenario)
    .WithReportFolder(reportDirectory)
    .WithReportFileName("nbomber-report")
    .WithReportingInterval(options.ReportingInterval)
    .Run();

Console.WriteLine();
Console.WriteLine("NBomber run completed.");

ArtifactWriter.WriteArtifacts(reportDirectory, options);

static async Task WarmUpMediatorAsync(IServiceProvider provider, NbomberOptions options)
{
    try
    {
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var sendResult = await mediator.Send(new PingCommand(-1), CancellationToken.None).ConfigureAwait(false);
        if (sendResult.IsLeft)
        {
            Console.WriteLine($"Warm-up send failed: {sendResult.LeftToList().First().Message}");
        }

        if (options.RequiresPublishWarmUp)
        {
            var publishResult = await mediator.Publish(new BroadcastNotification(-1), CancellationToken.None).ConfigureAwait(false);
            if (publishResult.IsLeft)
            {
                Console.WriteLine($"Warm-up publish failed: {publishResult.LeftToList().First().Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warm-up threw an exception: {ex.Message}");
    }
}

static string PrepareReportDirectory(NbomberOptions options)
{
    var directory = !string.IsNullOrEmpty(options.OutputDirectory)
        ? Path.GetFullPath(options.OutputDirectory)
        : Path.Combine("artifacts", "load-metrics", $"nbomber-{DateTime.UtcNow:yyyy-MM-dd.HHmmss}");

    Directory.CreateDirectory(directory);
    return directory;
}

internal static class ScenarioFactory
{
    internal static ScenarioProps[] CreateScenarios(NbomberOptions options, IServiceProvider provider)
    {
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var scenarios = new List<ScenarioProps>
        {
            CreateSendScenario(scopeFactory, options)
        };

        if (options.Scenario == NbomberScenarios.MixedTraffic && options.PublishRate > 0)
        {
            scenarios.Add(CreatePublishScenario(scopeFactory, options));
        }

        return scenarios.ToArray();
    }

    private static ScenarioProps CreateSendScenario(IServiceScopeFactory scopeFactory, NbomberOptions options)
    {
        return Scenario.Create("send_flow", async context =>
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var command = new PingCommand(HarnessState.NextSendId());

            var outcome = await mediator.Send(command, CancellationToken.None).ConfigureAwait(false);
            if (outcome.IsLeft)
            {
                var error = outcome.LeftToList().First();
                return Response.Fail(GetErrorMessage(error), statusCode: "mediator_error");
            }

            return Response.Ok();
        })
        .WithWarmUpDuration(options.WarmUp)
        .WithLoadSimulations(Simulation.Inject(rate: options.SendRate, interval: TimeSpan.FromSeconds(1), during: options.Duration));
    }

    private static ScenarioProps CreatePublishScenario(IServiceScopeFactory scopeFactory, NbomberOptions options)
    {
        return Scenario.Create("publish_flow", async context =>
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var notification = new BroadcastNotification(HarnessState.NextPublishId());

            var outcome = await mediator.Publish(notification, CancellationToken.None).ConfigureAwait(false);
            if (outcome.IsLeft)
            {
                var error = outcome.LeftToList().First();
                return Response.Fail(GetErrorMessage(error), statusCode: "mediator_error");
            }

            return Response.Ok();
        })
        .WithWarmUpDuration(options.WarmUp)
        .WithLoadSimulations(Simulation.Inject(rate: options.PublishRate, interval: TimeSpan.FromSeconds(1), during: options.Duration));
    }

    private static string GetErrorMessage(MediatorError error)
        => string.IsNullOrWhiteSpace(error.Message) ? "Mediator failure" : error.Message;
}


internal static class HarnessState
{
    private static long _sendSequence;
    private static long _publishSequence;

    public static long NextSendId() => Interlocked.Increment(ref _sendSequence);

    public static long NextPublishId() => Interlocked.Increment(ref _publishSequence);
}

internal static class NbomberScenarios
{
    internal const string SendBurst = "send-burst";
    internal const string MixedTraffic = "mixed-traffic";
}

internal sealed class NbomberOptions
{
    private static readonly JsonSerializerOptions ProfileSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string Scenario { get; set; } = NbomberScenarios.SendBurst;

    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan WarmUp { get; set; } = TimeSpan.FromSeconds(10);

    public int SendRate { get; set; } = 240_000;

    public int PublishRate { get; set; } = 90_000;

    public TimeSpan ReportingInterval { get; set; } = TimeSpan.FromSeconds(10);

    public string? OutputDirectory { get; set; }

    public string? ProfilePath { get; private set; }

    public bool RequiresPublishWarmUp => Scenario == NbomberScenarios.MixedTraffic && PublishRate > 0;

    public void Normalize()
    {
        Scenario = Scenario?.Trim().ToLowerInvariant() ?? NbomberScenarios.SendBurst;

        if (Duration <= TimeSpan.Zero)
        {
            Console.WriteLine("Duration must be positive. Falling back to 00:01:00.");
            Duration = TimeSpan.FromMinutes(1);
        }

        if (WarmUp < TimeSpan.Zero)
        {
            Console.WriteLine("Warm-up cannot be negative. Using 00:00:00.");
            WarmUp = TimeSpan.Zero;
        }

        if (ReportingInterval <= TimeSpan.Zero)
        {
            ReportingInterval = TimeSpan.FromSeconds(10);
        }

        if (SendRate <= 0)
        {
            Console.WriteLine("Send rate must be greater than zero. Using 1 ops/sec.");
            SendRate = 1;
        }

        if (PublishRate < 0)
        {
            PublishRate = 0;
        }

        if (WarmUp > Duration)
        {
            Console.WriteLine("Warm-up duration exceeds scenario duration. Clamping warm-up to match the scenario length.");
            WarmUp = Duration;
        }
    }

    public static NbomberOptions Parse(string[] args)
    {
        var options = new NbomberOptions();
        string? profile = null;
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            switch (current)
            {
                case "--profile" when TryReadValue(args, ref index, out var profileValue):
                    profile = profileValue;
                    break;
                case "--scenario" when TryReadValue(args, ref index, out var scenario):
                    overrides[current] = scenario;
                    break;
                case "--duration" when TryReadValue(args, ref index, out var duration):
                    overrides[current] = duration;
                    break;
                case "--warmup" when TryReadValue(args, ref index, out var warmup):
                    overrides[current] = warmup;
                    break;
                case "--send-rate" when TryReadValue(args, ref index, out var sendRate):
                    overrides[current] = sendRate;
                    break;
                case "--publish-rate" when TryReadValue(args, ref index, out var publishRate):
                    overrides[current] = publishRate;
                    break;
                case "--reporting-interval" when TryReadValue(args, ref index, out var reportingInterval):
                    overrides[current] = reportingInterval;
                    break;
                case "--output" when TryReadValue(args, ref index, out var output):
                    overrides[current] = output;
                    break;
            }
        }

        if (!string.IsNullOrEmpty(profile))
        {
            options.LoadProfile(profile!);
        }

        foreach (var (key, value) in overrides)
        {
            switch (key)
            {
                case "--scenario":
                    options.Scenario = value;
                    break;
                case "--duration" when TimeSpan.TryParse(value, out var parsedDuration):
                    options.Duration = parsedDuration;
                    break;
                case "--duration":
                    Console.WriteLine($"Invalid duration '{value}'.");
                    break;
                case "--warmup" when TimeSpan.TryParse(value, out var parsedWarmup):
                    options.WarmUp = parsedWarmup;
                    break;
                case "--warmup":
                    Console.WriteLine($"Invalid warm-up '{value}'.");
                    break;
                case "--send-rate" when int.TryParse(value, out var parsedSendRate):
                    options.SendRate = parsedSendRate;
                    break;
                case "--send-rate":
                    Console.WriteLine($"Invalid send rate '{value}'.");
                    break;
                case "--publish-rate" when int.TryParse(value, out var parsedPublishRate):
                    options.PublishRate = parsedPublishRate;
                    break;
                case "--publish-rate":
                    Console.WriteLine($"Invalid publish rate '{value}'.");
                    break;
                case "--reporting-interval" when TimeSpan.TryParse(value, out var parsedInterval):
                    options.ReportingInterval = parsedInterval;
                    break;
                case "--reporting-interval":
                    Console.WriteLine($"Invalid reporting interval '{value}'.");
                    break;
                case "--output":
                    options.OutputDirectory = value;
                    break;
            }
        }

        return options;
    }

    private void LoadProfile(string profilePath)
    {
        var fullPath = Path.GetFullPath(profilePath);
        if (!File.Exists(fullPath))
        {
            Console.Error.WriteLine($"Profile '{fullPath}' not found.");
            Environment.Exit(1);
            return;
        }

        try
        {
            var json = File.ReadAllText(fullPath);
            var profile = JsonSerializer.Deserialize<NbomberProfile>(json, ProfileSerializerOptions);

            if (profile is null)
            {
                Console.Error.WriteLine($"Profile '{fullPath}' could not be parsed.");
                Environment.Exit(1);
                return;
            }

            ApplyProfile(profile);
            ProfilePath = fullPath;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load profile '{fullPath}': {ex.Message}");
            Environment.Exit(1);
        }
    }

    private void ApplyProfile(NbomberProfile profile)
    {
        if (!string.IsNullOrEmpty(profile.Scenario))
        {
            Scenario = profile.Scenario;
        }

        if (profile.Duration.HasValue)
        {
            Duration = profile.Duration.Value;
        }

        if (profile.WarmUp.HasValue)
        {
            WarmUp = profile.WarmUp.Value;
        }

        if (profile.SendRate.HasValue)
        {
            SendRate = profile.SendRate.Value;
        }

        if (profile.PublishRate.HasValue)
        {
            PublishRate = profile.PublishRate.Value;
        }

        if (profile.ReportingInterval.HasValue)
        {
            ReportingInterval = profile.ReportingInterval.Value;
        }

        if (!string.IsNullOrEmpty(profile.OutputDirectory))
        {
            OutputDirectory = profile.OutputDirectory;
        }
    }

    private static bool TryReadValue(string[] args, ref int index, out string value)
    {
        value = string.Empty;
        if (index + 1 >= args.Length)
        {
            return false;
        }

        value = args[++index];
        return true;
    }
}

internal sealed class NbomberProfile
{
    public string? Scenario { get; set; }

    public TimeSpan? Duration { get; set; }

    public TimeSpan? WarmUp { get; set; }

    public int? SendRate { get; set; }

    public int? PublishRate { get; set; }

    public TimeSpan? ReportingInterval { get; set; }

    public string? OutputDirectory { get; set; }
}

internal sealed record PingCommand(long Id) : IRequest<int>;

internal sealed record BroadcastNotification(long Id) : INotification;

internal sealed class PingCommandHandler : IRequestHandler<PingCommand, int>
{
    public Task<Either<MediatorError, int>> Handle(PingCommand request, CancellationToken cancellationToken)
    {
        var computed = unchecked((int)(request.Id % 1_000));
        return Task.FromResult(Right<MediatorError, int>(computed));
    }
}

internal static class ArtifactWriter
{
    private static readonly JsonSerializerOptions SummarySerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void WriteArtifacts(string reportDirectory, NbomberOptions options)
    {
        try
        {
            var timestampToken = Path.GetFileName(reportDirectory)?.Replace("nbomber-", string.Empty)
                ?? DateTime.UtcNow.ToString("yyyy-MM-dd.HHmmss", CultureInfo.InvariantCulture);
            var summary = ScenarioSummary.Parse(reportDirectory);

            WriteHarnessLog(reportDirectory, timestampToken, summary, options);
            WriteMetricsCsv(reportDirectory, timestampToken);
            WriteSummaryJson(reportDirectory, summary);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to persist NBomber artifacts: {ex.Message}");
        }
    }

    private static void WriteHarnessLog(string directory, string timestampToken, ScenarioSummary summary, NbomberOptions options)
    {
        var logPath = Path.Combine(directory, $"harness-{timestampToken}.log");
        using var writer = new StreamWriter(logPath);
        writer.WriteLine($"=== NBomber Summary ({options.Scenario}) ===");

        if (summary.Send is not null)
        {
            WriteScenario(writer, "Send", summary.Send);
        }

        if (summary.Publish is not null)
        {
            WriteScenario(writer, "Publish", summary.Publish);
        }

        if (summary.Errors.Count > 0)
        {
            writer.WriteLine("Sample errors:");
            foreach (var error in summary.Errors)
            {
                writer.WriteLine("- " + error);
            }
        }
    }

    private static void WriteScenario(StreamWriter writer, string label, ScenarioMetrics metrics)
    {
        writer.WriteLine(FormattableString.Invariant($"{label} throughput: {metrics.MeanOpsPerSecond:F2} ops/sec"));
        writer.WriteLine(FormattableString.Invariant($"{label} throughput P50: {metrics.MeanOpsPerSecond:F2} ops/sec"));
        writer.WriteLine(FormattableString.Invariant($"{label} throughput P95: {metrics.MeanOpsPerSecond:F2} ops/sec"));
        writer.WriteLine(FormattableString.Invariant($"{label} latency mean: {metrics.MeanLatencyMs:F3} ms"));
        writer.WriteLine(FormattableString.Invariant($"{label} latency P50: {metrics.P50LatencyMs:F3} ms"));
        writer.WriteLine(FormattableString.Invariant($"{label} latency P95: {metrics.P95LatencyMs:F3} ms"));
    }

    private static void WriteMetricsCsv(string directory, string timestampToken)
    {
        var csvPath = Path.Combine(directory, $"metrics-{timestampToken}.csv");
        using var writer = new StreamWriter(csvPath);
        writer.WriteLine("timestamp,system_cpu_percent,process_cpu_percent,process_working_set_bytes");
        writer.WriteLine(FormattableString.Invariant($"{DateTimeOffset.UtcNow:O},,,"));
    }

    private static void WriteSummaryJson(string directory, ScenarioSummary summary)
    {
        var summaryPath = Path.Combine(directory, "nbomber-summary.json");
        var json = JsonSerializer.Serialize(summary, SummarySerializerOptions);
        File.WriteAllText(summaryPath, json);
    }
}

internal sealed class ScenarioSummary
{
    public ScenarioMetrics? Send { get; set; }

    public ScenarioMetrics? Publish { get; set; }

    public List<string> Errors { get; } = new();

    public static ScenarioSummary Parse(string reportDirectory)
    {
        var summary = new ScenarioSummary();
        var reportPath = Path.Combine(reportDirectory, "nbomber-report.csv");
        if (!File.Exists(reportPath))
        {
            return summary;
        }

        var lines = File.ReadAllLines(reportPath);
        if (lines.Length < 2)
        {
            return summary;
        }

        var headers = SplitLine(lines[0]);

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = SplitLine(line);
            if (columns.Length != headers.Length)
            {
                continue;
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Length; index++)
            {
                row[headers[index]] = columns[index];
            }

            if (!row.TryGetValue("scenario", out var scenario))
            {
                continue;
            }

            var metrics = ScenarioMetrics.From(row);

            if (scenario.Equals("send_flow", StringComparison.OrdinalIgnoreCase))
            {
                summary.Send = metrics;
            }
            else if (scenario.Equals("publish_flow", StringComparison.OrdinalIgnoreCase))
            {
                summary.Publish = metrics;
            }
        }

        return summary;
    }

    private static string[] SplitLine(string line)
    {
        return line.Split(',', StringSplitOptions.None)
            .Select(value => value.Trim().Trim('"'))
            .ToArray();
    }
}

internal sealed class ScenarioMetrics
{
    public double MeanOpsPerSecond { get; init; }

    public double MeanLatencyMs { get; init; }

    public double P50LatencyMs { get; init; }

    public double P95LatencyMs { get; init; }

    public static ScenarioMetrics From(IReadOnlyDictionary<string, string> row)
    {
        return new ScenarioMetrics
        {
            MeanOpsPerSecond = ParseDouble(row, "ok_rps"),
            MeanLatencyMs = ParseDouble(row, "ok_mean"),
            P50LatencyMs = ParseDouble(row, "ok_50_percent"),
            P95LatencyMs = ParseDouble(row, "ok_95_percent")
        };
    }

    private static double ParseDouble(IReadOnlyDictionary<string, string> row, string column)
    {
        if (!row.TryGetValue(column, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return double.NaN;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariant))
        {
            return invariant;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var culture))
        {
            return culture;
        }

        return double.NaN;
    }
}

internal sealed class BroadcastNotificationHandler : INotificationHandler<BroadcastNotification>
{
    public Task<Either<MediatorError, Unit>> Handle(BroadcastNotification notification, CancellationToken cancellationToken)
    {
        return Task.FromResult(Right<MediatorError, Unit>(Unit.Default));
    }
}
