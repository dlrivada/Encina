```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.39GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.990 μs |   0.1437 μs |  0.0079 μs |   1.00 |    0.00 |    1 | 0.0191 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     4.199 μs |   0.5094 μs |  0.0279 μs |   2.11 |    0.01 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    41.098 μs |   0.4615 μs |  0.0253 μs |  20.65 |    0.07 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    42.681 μs |   3.9947 μs |  0.2190 μs |  21.45 |    0.12 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,821.759 μs | 402.2734 μs | 22.0500 μs | 915.41 |   10.10 |    4 |      - |  52.58 KB |       28.76 |
