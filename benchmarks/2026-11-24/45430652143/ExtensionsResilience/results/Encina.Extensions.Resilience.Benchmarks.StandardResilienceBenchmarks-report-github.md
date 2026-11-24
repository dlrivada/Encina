```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.235 μs |   0.2283 μs |  0.0125 μs |   1.00 |    0.00 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.901 μs |   0.2673 μs |  0.0147 μs |   2.13 |    0.01 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_ConcurrentRequests         |    72.826 μs |   4.5452 μs |  0.2491 μs |  22.51 |    0.10 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_MultipleSequentialRequests |    73.064 μs |  28.1043 μs |  1.5405 μs |  22.58 |    0.42 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_WithRetry                  | 1,898.573 μs | 872.1377 μs | 47.8048 μs | 586.81 |   12.95 |    4 | 1.9531 |   52.6 KB |       28.77 |
