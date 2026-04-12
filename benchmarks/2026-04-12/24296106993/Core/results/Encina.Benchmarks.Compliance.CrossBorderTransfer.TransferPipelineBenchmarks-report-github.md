```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|----------------------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Pipeline: Block mode, adequate destination&#39;   |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Block mode, non-adequate (blocked)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Warn mode, non-adequate&#39;            |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Disabled mode (no validation)&#39;      |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: No attribute (skip branch)&#39;         |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  TransferPipelineBenchmarks.'Pipeline: Block mode, adequate destination': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  TransferPipelineBenchmarks.'Pipeline: Block mode, non-adequate (blocked)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  TransferPipelineBenchmarks.'Pipeline: Warn mode, non-adequate': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  TransferPipelineBenchmarks.'Pipeline: Disabled mode (no validation)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  TransferPipelineBenchmarks.'Pipeline: No attribute (skip branch)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
