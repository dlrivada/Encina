```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.063 μs |   1.208 μs |  0.0662 μs |   1.00 |    0.04 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.829 μs |   1.108 μs |  0.0607 μs |   1.86 |    0.06 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    38.265 μs |   6.524 μs |  0.3576 μs |  18.56 |    0.55 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    39.519 μs |   3.629 μs |  0.1989 μs |  19.17 |    0.55 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,734.652 μs | 695.579 μs | 38.1271 μs | 841.26 |   28.71 |    4 | 3.9063 |  75.46 KB |       41.28 |
