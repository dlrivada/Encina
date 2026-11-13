```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.533 μs |   0.2918 μs |  0.0160 μs |   1.00 |    0.01 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     5.049 μs |   0.0748 μs |  0.0041 μs |   1.99 |    0.01 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    49.813 μs |   0.4876 μs |  0.0267 μs |  19.67 |    0.11 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    51.424 μs |   2.2418 μs |  0.1229 μs |  20.30 |    0.12 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,802.612 μs | 230.9218 μs | 12.6576 μs | 711.65 |    5.82 |    4 | 1.9531 |   52.6 KB |       28.77 |
