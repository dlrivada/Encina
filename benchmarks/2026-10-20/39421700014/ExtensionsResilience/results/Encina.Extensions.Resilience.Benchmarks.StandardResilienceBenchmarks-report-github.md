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
| NoResilience_Baseline                         |     3.186 μs |     0.1162 μs |  0.0064 μs |   1.00 |    0.00 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.893 μs |     0.2745 μs |  0.0150 μs |   2.16 |    0.01 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    66.941 μs |     2.3261 μs |  0.1275 μs |  21.01 |    0.05 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    70.141 μs |     4.2903 μs |  0.2352 μs |  22.02 |    0.07 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,889.003 μs | 1,079.3336 μs | 59.1619 μs | 592.99 |   16.12 |    4 | 1.9531 |  52.59 KB |       28.77 |
