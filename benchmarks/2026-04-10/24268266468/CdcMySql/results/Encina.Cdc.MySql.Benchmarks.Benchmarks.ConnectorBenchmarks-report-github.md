```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean     | Error    | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |---------:|---------:|--------:|------:|--------:|----------:|------------:|
| GetCurrentPositionAsync | 517.8 μs | 143.5 μs | 7.87 μs |  1.00 |    0.02 |   6.38 KB |        1.00 |
