```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.486 μs |   0.2311 μs |  0.0127 μs |     1.00 |    0.01 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     3.346 μs |   0.9065 μs |  0.0497 μs |     2.25 |    0.03 |    2 | 0.0954 |   1.58 KB |        1.25 |
| StandardResilience_ConcurrentRequests         |    33.714 μs |  33.8350 μs |  1.8546 μs |    22.70 |    1.09 |    3 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_MultipleSequentialRequests |    34.459 μs |  99.5398 μs |  5.4561 μs |    23.20 |    3.19 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_WithRetry                  | 1,696.841 μs | 212.5331 μs | 11.6497 μs | 1,142.30 |   10.80 |    4 |      - |   6.56 KB |        5.21 |
