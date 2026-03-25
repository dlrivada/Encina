using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

var options = LoadMetricsOptions.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
Directory.CreateDirectory(options.OutputDirectory);

var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd.HHmmss", CultureInfo.InvariantCulture);
var logPath = Path.Combine(options.OutputDirectory, $"harness-{timestamp}.log");
var metricsPath = Path.Combine(options.OutputDirectory, $"metrics-{timestamp}.csv");

Console.WriteLine($"Starting load harness (duration {options.Duration:c}) with metrics capture...");

var harnessProcess = StartHarnessProcess(options, logPath, out var logWriter, out var logSync);
var sampler = new MetricsSampler(harnessProcess);
var samplingTask = sampler.RunAsync(options.Duration + TimeSpan.FromSeconds(5));

await harnessProcess.WaitForExitAsync().ConfigureAwait(false);
await samplingTask.ConfigureAwait(false);

lock (logSync)
{
    logWriter.Flush();
    logWriter.Dispose();
}

File.WriteAllLines(metricsPath, sampler.ToCsvLines());

Console.WriteLine($"Harness output written to {logPath}");
Console.WriteLine($"Metrics written to {metricsPath}");

var summaryFile = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
if (!string.IsNullOrEmpty(summaryFile))
{
    using var writer = File.AppendText(summaryFile);
    writer.WriteLine("### Load Harness Metrics");
    writer.WriteLine();
    writer.WriteLine($"- Log: `{logPath}`");
    writer.WriteLine($"- Metrics: `{metricsPath}`");
    writer.WriteLine();
}

return;

static Process StartHarnessProcess(LoadMetricsOptions options, string logPath, out StreamWriter logWriter, out object sync)
{
    var psi = new ProcessStartInfo("dotnet")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    psi.ArgumentList.Add("run");
    psi.ArgumentList.Add("--file");
    psi.ArgumentList.Add("scripts/run-load-harness.cs");

    var harnessArguments = options.BuildHarnessArguments();
    if (harnessArguments.Count > 0)
    {
        psi.ArgumentList.Add("--");
        foreach (var argument in harnessArguments)
        {
            psi.ArgumentList.Add(argument);
        }
    }

    var process = Process.Start(psi);
    if (process is null)
    {
        throw new InvalidOperationException("Failed to start load harness process.");
    }

    var writer = new StreamWriter(logPath, append: false, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    var syncObj = new object();

    process.EnableRaisingEvents = true;

    process.OutputDataReceived += (_, data) =>
    {
        if (data.Data is null)
        {
            return;
        }

        lock (syncObj)
        {
            Console.WriteLine(data.Data);
            writer.WriteLine(data.Data);
        }
    };

    process.ErrorDataReceived += (_, data) =>
    {
        if (data.Data is null)
        {
            return;
        }

        lock (syncObj)
        {
            Console.Error.WriteLine(data.Data);
            writer.WriteLine(data.Data);
        }
    };

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    logWriter = writer;
    sync = syncObj;

    return process;
}

internal sealed record LoadMetricsOptions(TimeSpan Duration, int? SendWorkers, int? PublishWorkers, string OutputDirectory)
{
    public static LoadMetricsOptions Parse(string[] args)
    {
        var duration = TimeSpan.FromMinutes(1);
        int? sendWorkers = null;
        int? publishWorkers = null;
        var output = Path.Combine("artifacts", "load-metrics");

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            switch (current)
            {
                case "--duration":
                    duration = TimeSpan.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--send-workers":
                    sendWorkers = int.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--publish-workers":
                    publishWorkers = int.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--output":
                    output = ReadRequired(args, ref index);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{current}'.");
            }
        }

        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Duration must be greater than zero.");
        }

        return new LoadMetricsOptions(duration, sendWorkers, publishWorkers, output);
    }

    public List<string> BuildHarnessArguments()
    {
        var arguments = new List<string>
        {
            "--duration",
            Duration.ToString("c", CultureInfo.InvariantCulture)
        };

        if (SendWorkers.HasValue)
        {
            arguments.Add("--send-workers");
            arguments.Add(SendWorkers.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (PublishWorkers.HasValue)
        {
            arguments.Add("--publish-workers");
            arguments.Add(PublishWorkers.Value.ToString(CultureInfo.InvariantCulture));
        }

        return arguments;
    }

    private static string ReadRequired(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for argument '{args[index]}'.");
        }

        index++;
        return args[index];
    }
}

internal sealed class MetricsSampler
{
    private readonly Process _process;
    private readonly List<MetricSample> _samples = new();
    private readonly object _sync = new();

    private DateTime? _lastTimestamp;
    private TimeSpan _lastProcessCpu;

    public MetricsSampler(Process process)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
    }

    public Task RunAsync(TimeSpan maxDuration)
    {
        return Task.Run(async () => await SampleAsync(maxDuration).ConfigureAwait(false));
    }

    public IEnumerable<string> ToCsvLines()
    {
        var lines = new List<string>(_samples.Count + 1)
        {
            "timestamp,system_cpu_percent,process_cpu_percent,process_working_set_bytes"
        };

        lock (_sync)
        {
            foreach (var sample in _samples)
            {
                lines.Add(sample.ToCsv());
            }
        }

        return lines;
    }

    private async Task SampleAsync(TimeSpan maxDuration)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < maxDuration)
        {
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

            var timestamp = DateTime.UtcNow;
            const double systemCpu = double.NaN;

            var processCpu = 0.0;
            long workingSet = 0;

            if (!_process.HasExited)
            {
                try
                {
                    _process.Refresh();
                    workingSet = _process.WorkingSet64;
                    var totalProcessorTime = _process.TotalProcessorTime;

                    if (_lastTimestamp.HasValue)
                    {
                        var deltaCpu = totalProcessorTime - _lastProcessCpu;
                        var deltaTime = timestamp - _lastTimestamp.Value;

                        if (deltaTime.TotalSeconds > 0 && deltaCpu > TimeSpan.Zero)
                        {
                            processCpu = deltaCpu.TotalSeconds / deltaTime.TotalSeconds * 100.0 / Environment.ProcessorCount;
                        }
                    }

                    _lastProcessCpu = totalProcessorTime;
                }
                catch
                {
                    // Ignore sampling errors after the process exits.
                }
            }

            _lastTimestamp = timestamp;

            lock (_sync)
            {
                _samples.Add(new MetricSample(timestamp, systemCpu, processCpu, workingSet));
            }

            if (_process.HasExited)
            {
                break;
            }
        }
    }

    private readonly record struct MetricSample(DateTime Timestamp, double SystemCpuPercent, double ProcessCpuPercent, long WorkingSetBytes)
    {
        public string ToCsv()
        {
            var systemCpu = double.IsNaN(SystemCpuPercent)
                ? string.Empty
                : SystemCpuPercent.ToString("F2", CultureInfo.InvariantCulture);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2:F2},{3}",
                Timestamp.ToString("o", CultureInfo.InvariantCulture),
                systemCpu,
                ProcessCpuPercent,
                WorkingSetBytes);
        }
    }
}
