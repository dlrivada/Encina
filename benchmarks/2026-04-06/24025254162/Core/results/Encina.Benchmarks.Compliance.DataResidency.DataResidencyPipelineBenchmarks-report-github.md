```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|---------------------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Pipeline: Block mode, allowed region&#39;        |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Block mode, non-allowed (blocked)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Warn mode, non-allowed&#39;            |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Disabled mode (no validation)&#39;     |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: No attribute (skip branch)&#39;        |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  DataResidencyPipelineBenchmarks.'Pipeline: Block mode, allowed region': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  DataResidencyPipelineBenchmarks.'Pipeline: Block mode, non-allowed (blocked)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  DataResidencyPipelineBenchmarks.'Pipeline: Warn mode, non-allowed': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  DataResidencyPipelineBenchmarks.'Pipeline: Disabled mode (no validation)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  DataResidencyPipelineBenchmarks.'Pipeline: No attribute (skip branch)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
