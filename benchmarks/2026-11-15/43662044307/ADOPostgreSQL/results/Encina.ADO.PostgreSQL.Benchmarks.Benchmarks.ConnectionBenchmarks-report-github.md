```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host] : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method       | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------- |-----:|------:|------:|--------:|------------:|
| OpenAndClose |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  ConnectionBenchmarks.OpenAndClose: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
