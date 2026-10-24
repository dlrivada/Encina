```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|--------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 228.5 μs | 31.83 μs | 1.74 μs |  1.00 |    0.01 | 1.2207 |      - |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 498.7 μs | 58.10 μs | 3.18 μs |  2.18 |    0.02 | 5.3711 | 0.4883 |  93.64 KB |        4.50 |
