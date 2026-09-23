```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|----------------------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Pipeline: Block mode, adequate destination&#39;   |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Block mode, non-adequate (blocked)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Warn mode, non-adequate&#39;            |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Disabled mode (no validation)&#39;      |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: No attribute (skip branch)&#39;         |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  TransferPipelineBenchmarks.'Pipeline: Block mode, adequate destination': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  TransferPipelineBenchmarks.'Pipeline: Block mode, non-adequate (blocked)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  TransferPipelineBenchmarks.'Pipeline: Warn mode, non-adequate': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  TransferPipelineBenchmarks.'Pipeline: Disabled mode (no validation)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  TransferPipelineBenchmarks.'Pipeline: No attribute (skip branch)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
