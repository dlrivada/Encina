```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.875 μs |   0.3283 μs |  0.0180 μs |   1.00 |    0.01 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.691 μs |   0.8727 μs |  0.0478 μs |   1.97 |    0.03 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    36.314 μs |   8.9683 μs |  0.4916 μs |  19.37 |    0.28 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    39.637 μs |   3.4212 μs |  0.1875 μs |  21.14 |    0.20 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,763.967 μs | 584.0189 μs | 32.0120 μs | 940.67 |   16.74 |    4 | 1.9531 |  52.51 KB |       28.72 |
