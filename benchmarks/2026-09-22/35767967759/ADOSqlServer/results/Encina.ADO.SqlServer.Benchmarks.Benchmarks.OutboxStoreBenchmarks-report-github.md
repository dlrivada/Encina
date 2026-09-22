```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 744.8 μs | 2,103.8 μs | 115.31 μs |  1.01 |    0.19 | 0.9766 |      - |  16.51 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 607.2 μs |   174.8 μs |   9.58 μs |  0.83 |    0.10 | 6.8359 | 0.9766 | 124.49 KB |        7.54 |
