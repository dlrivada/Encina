```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|--------------------------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Pipeline: Block mode, with [Anonymize] attribute&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Warn mode, with [Anonymize] attribute&#39;  |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Disabled mode (no transformation)&#39;      |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: No attribute (skip branch)&#39;             |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  AnonymizationPipelineBenchmarks.'Pipeline: Block mode, with [Anonymize] attribute': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  AnonymizationPipelineBenchmarks.'Pipeline: Warn mode, with [Anonymize] attribute': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  AnonymizationPipelineBenchmarks.'Pipeline: Disabled mode (no transformation)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  AnonymizationPipelineBenchmarks.'Pipeline: No attribute (skip branch)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
