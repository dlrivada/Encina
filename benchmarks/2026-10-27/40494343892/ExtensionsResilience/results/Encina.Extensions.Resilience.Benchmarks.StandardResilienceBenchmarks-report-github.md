```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.598 μs |   0.0440 μs |  0.0024 μs |   1.00 |    0.00 |    1 | 0.0725 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.355 μs |   0.1382 μs |  0.0076 μs |   2.45 |    0.00 |    2 | 0.0839 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    64.121 μs |   2.5809 μs |  0.1415 μs |  24.68 |    0.05 |    3 | 0.7324 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    66.904 μs |   2.1340 μs |  0.1170 μs |  25.75 |    0.04 |    3 | 0.8545 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,933.328 μs | 569.9184 μs | 31.2392 μs | 744.18 |   10.43 |    4 | 1.9531 |   52.6 KB |       28.77 |
