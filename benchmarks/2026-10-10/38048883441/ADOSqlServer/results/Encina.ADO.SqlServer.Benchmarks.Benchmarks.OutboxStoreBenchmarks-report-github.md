```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 662.3 μs | 973.31 μs | 53.35 μs |  1.00 |    0.10 | 0.9766 |      - |   16.5 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 677.2 μs |  84.88 μs |  4.65 μs |  1.03 |    0.07 | 6.8359 | 0.9766 |  124.5 KB |        7.54 |
