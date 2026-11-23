```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.327 μs |   0.0782 μs |  0.0043 μs |   1.00 |    0.00 |    1 | 0.0191 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     4.996 μs |   0.1746 μs |  0.0096 μs |   2.15 |    0.00 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    48.584 μs |   5.5328 μs |  0.3033 μs |  20.88 |    0.12 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    50.385 μs |   8.2169 μs |  0.4504 μs |  21.65 |    0.17 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,834.537 μs | 585.8922 μs | 32.1147 μs | 788.42 |   12.02 |    4 |      - |  52.63 KB |       28.79 |
