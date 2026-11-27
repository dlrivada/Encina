```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.538 μs |   0.5672 μs |  0.0311 μs |   1.00 |    0.02 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     5.026 μs |   0.1336 μs |  0.0073 μs |   1.98 |    0.02 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    50.287 μs |   5.3325 μs |  0.2923 μs |  19.82 |    0.23 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    53.372 μs |   4.0119 μs |  0.2199 μs |  21.03 |    0.24 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,778.719 μs | 507.1067 μs | 27.7962 μs | 700.96 |   12.08 |    4 | 1.9531 |  52.57 KB |       28.76 |
