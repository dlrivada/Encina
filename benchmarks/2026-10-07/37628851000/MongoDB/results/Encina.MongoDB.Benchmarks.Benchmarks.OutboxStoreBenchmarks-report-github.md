```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 298.7 μs | 203.6 μs | 11.16 μs |  1.00 |    0.05 |      - |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 661.2 μs | 197.8 μs | 10.84 μs |  2.22 |    0.08 | 0.9766 |  93.64 KB |        4.50 |
