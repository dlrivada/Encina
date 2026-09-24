```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method          | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|---------------- |-----:|------:|------:|--------:|------------:|
| AddAsync_Single |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  OutboxStoreBenchmarks.AddAsync_Single: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
