```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host] : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method   | Mean | Error |
|--------- |-----:|------:|
| AddAsync |   NA |    NA |

Benchmarks with issues:
  FunctionalRepositoryBenchmarks.AddAsync: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
