```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.862 μs |   1.1234 μs |  0.0616 μs |   1.00 |    0.04 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.774 μs |   0.1589 μs |  0.0087 μs |   2.03 |    0.06 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_ConcurrentRequests         |    37.168 μs |   1.7754 μs |  0.0973 μs |  19.98 |    0.57 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_MultipleSequentialRequests |    37.258 μs |  22.0101 μs |  1.2064 μs |  20.03 |    0.80 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_WithRetry                  | 1,686.178 μs | 409.3673 μs | 22.4388 μs | 906.33 |   27.72 |    4 | 1.9531 |  52.56 KB |       28.75 |
