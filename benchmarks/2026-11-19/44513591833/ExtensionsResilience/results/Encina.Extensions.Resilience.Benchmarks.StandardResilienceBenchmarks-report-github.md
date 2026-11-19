```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.172 μs |     0.1082 μs |  0.0059 μs |   1.00 |    0.00 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     7.084 μs |     0.2056 μs |  0.0113 μs |   2.23 |    0.00 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    68.749 μs |     7.0157 μs |  0.3846 μs |  21.68 |    0.11 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    74.104 μs |     6.9847 μs |  0.3829 μs |  23.36 |    0.11 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,863.215 μs | 1,059.9464 μs | 58.0992 μs | 587.46 |   15.89 |    4 |      - |  52.57 KB |       28.75 |
