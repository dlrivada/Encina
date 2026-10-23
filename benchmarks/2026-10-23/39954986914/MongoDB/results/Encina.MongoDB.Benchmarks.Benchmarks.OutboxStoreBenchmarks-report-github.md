```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 280.5 μs | 406.9 μs | 22.30 μs |  1.00 |    0.10 |      - |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 661.0 μs | 505.6 μs | 27.71 μs |  2.37 |    0.19 | 0.9766 |  93.64 KB |        4.50 |
