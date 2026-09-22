```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.947 μs |     0.1033 μs |  0.0057 μs |   1.00 |    0.00 |    1 | 0.0496 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.719 μs |     0.2433 μs |  0.0133 μs |   2.94 |    0.01 |    2 | 0.0610 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    55.589 μs |     0.6954 μs |  0.0381 μs |  28.55 |    0.07 |    3 | 0.5493 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    60.133 μs |     5.5648 μs |  0.3050 μs |  30.89 |    0.16 |    3 | 0.6104 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,746.741 μs | 1,339.1732 μs | 73.4046 μs | 897.18 |   32.73 |    4 |      - |   6.55 KB |        5.21 |
