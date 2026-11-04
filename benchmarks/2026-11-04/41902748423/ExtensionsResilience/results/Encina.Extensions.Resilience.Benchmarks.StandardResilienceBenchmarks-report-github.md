```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.320 μs |   0.9466 μs |  0.0519 μs |   1.00 |    0.02 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.961 μs |   0.1714 μs |  0.0094 μs |   2.10 |    0.03 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    71.528 μs |   0.9444 μs |  0.0518 μs |  21.55 |    0.29 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    71.549 μs |   6.1834 μs |  0.3389 μs |  21.56 |    0.30 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,908.108 μs | 872.8459 μs | 47.8436 μs | 574.84 |   14.67 |    4 |      - |  52.49 KB |       28.71 |
