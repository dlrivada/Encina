```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 2,731.6 μs | 7,883.7 μs | 432.13 μs |  1.02 |    0.20 |      - |   6.82 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 |   358.2 μs |   125.7 μs |   6.89 μs |  0.13 |    0.02 | 2.9297 |  48.52 KB |        7.12 |
