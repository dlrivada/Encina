```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|--------------------- |-----:|------:|------:|--------:|------------:|
| OpenAndExecuteScalar |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  ConnectionBenchmarks.OpenAndExecuteScalar: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
