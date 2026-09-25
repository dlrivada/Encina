```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|--------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 120.3 μs | 64.20 μs | 3.52 μs |  1.00 |    0.04 | 0.2441 |      - |   5.28 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 243.6 μs | 93.65 μs | 5.13 μs |  2.03 |    0.06 | 2.4414 | 0.2441 |  40.16 KB |        7.60 |
