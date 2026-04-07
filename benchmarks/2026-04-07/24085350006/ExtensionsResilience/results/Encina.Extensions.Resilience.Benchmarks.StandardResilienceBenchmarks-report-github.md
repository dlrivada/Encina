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
| NoResilience_Baseline                         |     2.480 μs |   0.1204 μs |  0.0066 μs |   1.00 |    0.00 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.781 μs |   0.1963 μs |  0.0108 μs |   2.33 |    0.01 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    58.791 μs |   3.5679 μs |  0.1956 μs |  23.71 |    0.09 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    61.031 μs |   6.8529 μs |  0.3756 μs |  24.61 |    0.14 |    3 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,737.682 μs | 538.9508 μs | 29.5417 μs | 700.79 |   10.44 |    4 |      - |   6.59 KB |        5.24 |
