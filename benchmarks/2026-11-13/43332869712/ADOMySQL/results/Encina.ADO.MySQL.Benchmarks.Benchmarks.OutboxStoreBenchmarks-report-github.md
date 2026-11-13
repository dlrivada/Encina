```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 552.0 μs |  94.96 μs |  5.20 μs |  1.00 |    0.01 |      - |   6.87 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 369.0 μs | 228.36 μs | 12.52 μs |  0.67 |    0.02 | 2.9297 |  48.57 KB |        7.07 |
