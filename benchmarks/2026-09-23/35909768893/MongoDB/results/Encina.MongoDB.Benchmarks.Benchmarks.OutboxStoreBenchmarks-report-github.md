```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 241.6 μs |  46.92 μs |  2.57 μs |  1.00 |    0.01 | 0.9766 |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 537.0 μs | 380.13 μs | 20.84 μs |  2.22 |    0.08 | 4.8828 |  93.64 KB |        4.50 |
