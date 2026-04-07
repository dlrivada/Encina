```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                      | Mean | Error | Ratio | RatioSD | Rank | Alloc Ratio |
|-------------------------------------------- |-----:|------:|------:|--------:|-----:|------------:|
| &#39;Pipeline: Enforce, registered (compliant)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: Enforce, unregistered (blocked)&#39; |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: WarnOnly mode&#39;                   |   NA |    NA |     ? |       ? |    ? |           ? |
| &#39;Pipeline: No attribute (skip branch)&#39;      |   NA |    NA |     ? |       ? |    ? |           ? |

Benchmarks with issues:
  GDPRPipelineBenchmarks.'Pipeline: Enforce, registered (compliant)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  GDPRPipelineBenchmarks.'Pipeline: Enforce, unregistered (blocked)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  GDPRPipelineBenchmarks.'Pipeline: WarnOnly mode': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
  GDPRPipelineBenchmarks.'Pipeline: No attribute (skip branch)': Dry(IterationCount=1, LaunchCount=1, RunStrategy=ColdStart, UnrollFactor=1, WarmupCount=1)
