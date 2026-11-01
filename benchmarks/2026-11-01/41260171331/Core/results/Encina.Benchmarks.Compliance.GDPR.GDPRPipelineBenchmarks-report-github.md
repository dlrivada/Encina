```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                      | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|-------------------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Pipeline: Enforce, registered (compliant)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Enforce, unregistered (blocked)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: WarnOnly mode&#39;                   |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: No attribute (skip branch)&#39;      |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  GDPRPipelineBenchmarks.'Pipeline: Enforce, registered (compliant)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  GDPRPipelineBenchmarks.'Pipeline: Enforce, unregistered (blocked)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  GDPRPipelineBenchmarks.'Pipeline: WarnOnly mode': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
  GDPRPipelineBenchmarks.'Pipeline: No attribute (skip branch)': MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
