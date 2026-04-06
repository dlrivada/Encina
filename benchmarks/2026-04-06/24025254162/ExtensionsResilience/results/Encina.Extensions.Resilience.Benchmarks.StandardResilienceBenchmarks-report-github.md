```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.408 μs |   0.2279 μs |  0.0125 μs |   1.00 |    0.01 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.843 μs |   0.9841 μs |  0.0539 μs |   2.43 |    0.02 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    56.483 μs |   1.6736 μs |  0.0917 μs |  23.45 |    0.11 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    59.257 μs |   1.4611 μs |  0.0801 μs |  24.61 |    0.11 |    3 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,760.357 μs | 573.1326 μs | 31.4153 μs | 730.99 |   11.77 |    4 |      - |   6.57 KB |        5.22 |
