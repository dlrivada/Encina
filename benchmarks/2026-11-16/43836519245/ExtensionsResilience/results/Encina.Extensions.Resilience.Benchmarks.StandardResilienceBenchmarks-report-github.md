```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.121 μs |   1.537 μs |  0.0842 μs |   1.00 |    0.05 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     4.004 μs |   1.654 μs |  0.0907 μs |   1.89 |    0.07 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    38.537 μs |  20.091 μs |  1.1013 μs |  18.19 |    0.76 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    40.418 μs |   4.110 μs |  0.2253 μs |  19.08 |    0.65 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,688.816 μs | 200.843 μs | 11.0089 μs | 797.05 |   27.38 |    4 | 1.9531 |  52.57 KB |       28.75 |
