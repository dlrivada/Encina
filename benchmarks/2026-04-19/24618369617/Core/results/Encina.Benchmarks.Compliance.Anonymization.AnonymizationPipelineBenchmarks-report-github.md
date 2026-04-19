```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                             | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|--------------------------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Pipeline: Block mode, with [Anonymize] attribute&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Warn mode, with [Anonymize] attribute&#39;  |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Disabled mode (no transformation)&#39;      |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: No attribute (skip branch)&#39;             |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  AnonymizationPipelineBenchmarks.'Pipeline: Block mode, with [Anonymize] attribute': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  AnonymizationPipelineBenchmarks.'Pipeline: Warn mode, with [Anonymize] attribute': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  AnonymizationPipelineBenchmarks.'Pipeline: Disabled mode (no transformation)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  AnonymizationPipelineBenchmarks.'Pipeline: No attribute (skip branch)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
