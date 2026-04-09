```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

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
