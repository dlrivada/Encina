```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.733 μs |   0.2310 μs |  0.0127 μs |     1.00 |    0.01 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.479 μs |   0.1332 μs |  0.0073 μs |     2.01 |    0.01 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    35.419 μs |  27.7022 μs |  1.5184 μs |    20.44 |    0.77 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    35.655 μs |   0.8825 μs |  0.0484 μs |    20.58 |    0.13 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,750.414 μs | 527.0694 μs | 28.8905 μs | 1,010.12 |   15.78 |    4 | 1.9531 |  52.59 KB |       28.77 |
