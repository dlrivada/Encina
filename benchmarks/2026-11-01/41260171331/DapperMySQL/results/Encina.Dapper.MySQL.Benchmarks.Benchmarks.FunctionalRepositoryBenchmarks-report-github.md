```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method   | Mean | Error |
|--------- |-----:|------:|
| AddAsync |   NA |    NA |

Benchmarks with issues:
  FunctionalRepositoryBenchmarks.AddAsync: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
