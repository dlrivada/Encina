```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|--------:|------:|-------:|----------:|------------:|
| AddAsync_Single                  | 567.8 μs |  67.48 μs | 3.70 μs |  1.00 |      - |   6.82 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 358.9 μs | 152.42 μs | 8.35 μs |  0.63 | 2.9297 |  48.52 KB |        7.12 |
