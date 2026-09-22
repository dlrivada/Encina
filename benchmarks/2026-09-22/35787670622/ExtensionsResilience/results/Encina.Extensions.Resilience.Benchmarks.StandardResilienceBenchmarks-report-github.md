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
| NoResilience_Baseline                         |     1.513 μs |   0.2724 μs |  0.0149 μs |     1.00 |    0.01 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     3.475 μs |   0.4360 μs |  0.0239 μs |     2.30 |    0.02 |    2 | 0.0954 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    34.201 μs |   7.9893 μs |  0.4379 μs |    22.61 |    0.32 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    35.042 μs |   4.5870 μs |  0.2514 μs |    23.17 |    0.25 |    3 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,688.935 μs | 426.3984 μs | 23.3723 μs | 1,116.52 |   16.45 |    4 |      - |   6.56 KB |        5.22 |
