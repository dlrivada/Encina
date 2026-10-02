```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.641 μs |   0.1993 μs |  0.0109 μs |   1.00 |    0.01 |    1 | 0.0954 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     6.108 μs |   0.8461 μs |  0.0464 μs |   2.31 |    0.02 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    62.645 μs |   2.8097 μs |  0.1540 μs |  23.72 |    0.10 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    63.632 μs |   1.1668 μs |  0.0640 μs |  24.09 |    0.09 |    3 | 1.0986 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,791.402 μs | 644.1714 μs | 35.3092 μs | 678.26 |   11.83 |    4 |      - |   6.79 KB |        4.24 |
