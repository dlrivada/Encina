```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.959 μs |   0.5165 μs |  0.0283 μs |   1.00 |    0.02 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.933 μs |   1.8076 μs |  0.0991 μs |   2.01 |    0.05 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    38.147 μs |  42.1411 μs |  2.3099 μs |  19.47 |    1.05 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    39.159 μs |   2.0315 μs |  0.1114 μs |  19.99 |    0.26 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,778.621 μs | 468.0820 μs | 25.6572 μs | 908.00 |   16.07 |    4 | 3.9063 |  75.64 KB |       41.38 |
