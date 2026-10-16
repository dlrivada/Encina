```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.232 μs |     0.0577 μs |  0.0032 μs |   1.00 |    0.00 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.703 μs |     0.2860 μs |  0.0157 μs |   2.07 |    0.00 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    69.374 μs |     4.5541 μs |  0.2496 μs |  21.47 |    0.07 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    70.560 μs |     2.7245 μs |  0.1493 μs |  21.83 |    0.04 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,979.404 μs | 1,771.6626 μs | 97.1108 μs | 612.46 |   26.03 |    4 |      - |  52.99 KB |       27.46 |
