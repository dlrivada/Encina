```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.44GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.657 μs |     0.9339 μs |  0.0512 μs |   1.00 |    0.02 |    1 | 0.0725 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.494 μs |     0.5232 μs |  0.0287 μs |   2.44 |    0.04 |    2 | 0.0839 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    63.361 μs |     3.8510 μs |  0.2111 μs |  23.85 |    0.40 |    3 | 0.7324 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    69.674 μs |     4.3304 μs |  0.2374 μs |  26.23 |    0.44 |    3 | 0.8545 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,860.721 μs | 1,036.4418 μs | 56.8109 μs | 700.42 |   21.83 |    4 |      - |  52.55 KB |       28.74 |
