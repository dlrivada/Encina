```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                           | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 509.1 μs | 6.83 μs | 9.12 μs |  1.00 |    0.02 |      - |   6.82 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 329.1 μs | 3.30 μs | 4.93 μs |  0.65 |    0.01 | 2.9297 |  48.52 KB |        7.12 |
