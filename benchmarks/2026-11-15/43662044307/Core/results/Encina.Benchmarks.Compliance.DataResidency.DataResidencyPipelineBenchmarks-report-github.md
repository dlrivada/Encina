```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|---------------------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Pipeline: Block mode, allowed region&#39;        |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Block mode, non-allowed (blocked)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Warn mode, non-allowed&#39;            |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Disabled mode (no validation)&#39;     |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: No attribute (skip branch)&#39;        |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  DataResidencyPipelineBenchmarks.'Pipeline: Block mode, allowed region': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  DataResidencyPipelineBenchmarks.'Pipeline: Block mode, non-allowed (blocked)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  DataResidencyPipelineBenchmarks.'Pipeline: Warn mode, non-allowed': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  DataResidencyPipelineBenchmarks.'Pipeline: Disabled mode (no validation)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  DataResidencyPipelineBenchmarks.'Pipeline: No attribute (skip branch)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
