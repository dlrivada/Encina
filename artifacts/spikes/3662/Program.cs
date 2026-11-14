using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using RopUnions.Spike;

if (args.Length > 0 && args[0] == "--probe")
{
    Probe.Run();
    return;
}

// BenchmarkDotNet 0.15.8 does not recognise net11.0 (RuntimeMoniker.NotRecognized -> NotImplementedException
// in DotNetSdkValidator), so the out-of-process CsProj toolchain cannot run. The in-process emit toolchain
// measures the same JIT-compiled code in the host process. Job = ShortRun (3 warmups, 3 iterations, 1 launch).
var artifacts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "performance", "spike-3662"));
var config = DefaultConfig.Instance
    .WithArtifactsPath(artifacts)
    .AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance));
BenchmarkSwitcher.FromAssembly(typeof(Probe).Assembly).Run(args, config);
