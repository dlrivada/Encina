```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.380 μs |   0.0809 μs |  0.0044 μs |   1.00 |    0.00 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.754 μs |   0.2702 μs |  0.0148 μs |   2.42 |    0.01 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    57.711 μs |   2.0196 μs |  0.1107 μs |  24.25 |    0.06 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    61.118 μs |   3.5374 μs |  0.1939 μs |  25.68 |    0.08 |    3 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,771.654 μs | 726.5473 μs | 39.8245 μs | 744.44 |   14.54 |    4 |      - |   6.54 KB |        5.20 |
