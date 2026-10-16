```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 249.9 μs | 227.6 μs | 12.47 μs |  1.00 |    0.06 | 0.9766 |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 545.3 μs | 538.4 μs | 29.51 μs |  2.19 |    0.14 | 4.8828 |  93.64 KB |        4.50 |
