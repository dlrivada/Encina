```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 216.9 μs | 156.3 μs |  8.57 μs |  1.00 |    0.05 | 1.2207 |      - |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 496.4 μs | 198.2 μs | 10.86 μs |  2.29 |    0.09 | 5.3711 | 0.4883 |  93.64 KB |        4.50 |
