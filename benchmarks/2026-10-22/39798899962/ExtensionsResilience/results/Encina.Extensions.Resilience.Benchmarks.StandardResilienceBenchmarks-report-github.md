```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.921 μs |   0.7194 μs |  0.0394 μs |   1.00 |    0.03 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.755 μs |   0.9661 μs |  0.0530 μs |   1.96 |    0.04 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    37.526 μs |  20.0377 μs |  1.0983 μs |  19.54 |    0.61 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    38.278 μs |  30.2057 μs |  1.6557 μs |  19.93 |    0.83 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,694.815 μs | 190.5384 μs | 10.4441 μs | 882.54 |   16.54 |    4 | 1.9531 |  52.55 KB |       28.74 |
