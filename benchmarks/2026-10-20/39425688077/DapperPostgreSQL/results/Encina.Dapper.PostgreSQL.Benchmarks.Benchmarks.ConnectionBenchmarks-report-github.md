```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host] : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|--------------------- |-----:|------:|------:|--------:|------------:|
| OpenAndExecuteScalar |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  ConnectionBenchmarks.OpenAndExecuteScalar: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
