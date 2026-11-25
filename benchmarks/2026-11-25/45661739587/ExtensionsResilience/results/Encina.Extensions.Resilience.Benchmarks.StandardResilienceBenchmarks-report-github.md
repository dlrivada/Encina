```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.835 μs |   0.8070 μs |  0.0442 μs |   1.00 |    0.03 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.701 μs |   0.8832 μs |  0.0484 μs |   2.02 |    0.05 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    37.308 μs |   9.1894 μs |  0.5037 μs |  20.34 |    0.48 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    39.873 μs |  14.3685 μs |  0.7876 μs |  21.74 |    0.58 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,690.894 μs | 632.0690 μs | 34.6458 μs | 921.92 |   25.08 |    4 | 1.9531 |  52.57 KB |       28.75 |
