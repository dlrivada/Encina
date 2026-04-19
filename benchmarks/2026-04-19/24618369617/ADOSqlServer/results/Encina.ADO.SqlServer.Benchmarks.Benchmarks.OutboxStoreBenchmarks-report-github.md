```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.72GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host] : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method          | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|---------------- |-----:|------:|------:|--------:|------------:|
| AddAsync_Single |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  OutboxStoreBenchmarks.AddAsync_Single: MediumRun(IterationCount=15, LaunchCount=2, WarmupCount=10)
