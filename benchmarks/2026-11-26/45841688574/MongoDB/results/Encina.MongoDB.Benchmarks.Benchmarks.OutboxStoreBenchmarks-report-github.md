```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.34GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 229.0 μs |    57.02 μs |  3.13 μs |  1.00 |    0.02 | 1.2207 |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 572.0 μs | 1,106.58 μs | 60.66 μs |  2.50 |    0.23 | 4.8828 |  93.64 KB |        4.50 |
