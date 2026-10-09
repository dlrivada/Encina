```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.037 μs |     0.0649 μs |  0.0036 μs |   1.00 |    0.00 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.771 μs |     0.2711 μs |  0.0149 μs |   2.23 |    0.00 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    65.889 μs |     7.8765 μs |  0.4317 μs |  21.70 |    0.13 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    69.592 μs |     3.3164 μs |  0.1818 μs |  22.92 |    0.06 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,954.614 μs | 1,235.5428 μs | 67.7243 μs | 643.62 |   19.32 |    4 |      - |  52.98 KB |       27.46 |
