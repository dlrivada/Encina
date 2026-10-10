```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method   | Mean | Error |
|--------- |-----:|------:|
| AddAsync |   NA |    NA |

Benchmarks with issues:
  FunctionalRepositoryBenchmarks.AddAsync: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
