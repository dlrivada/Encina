```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 866.9 μs | 1,097.36 μs | 60.15 μs |  1.00 |    0.08 |      - |   6.87 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 358.8 μs |    47.81 μs |  2.62 μs |  0.42 |    0.02 | 2.9297 |  48.58 KB |        7.07 |
