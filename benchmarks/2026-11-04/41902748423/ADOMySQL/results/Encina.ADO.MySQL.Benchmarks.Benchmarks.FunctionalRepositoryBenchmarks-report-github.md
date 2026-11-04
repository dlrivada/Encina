```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method   | Mean | Error |
|--------- |-----:|------:|
| AddAsync |   NA |    NA |

Benchmarks with issues:
  FunctionalRepositoryBenchmarks.AddAsync: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
