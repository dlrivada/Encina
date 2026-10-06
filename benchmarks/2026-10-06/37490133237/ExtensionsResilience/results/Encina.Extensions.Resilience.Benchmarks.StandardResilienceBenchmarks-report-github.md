```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.924 μs |   0.3809 μs |  0.0209 μs |   1.00 |    0.01 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.466 μs |   0.2329 μs |  0.0128 μs |   2.21 |    0.01 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    65.825 μs |   4.8429 μs |  0.2655 μs |  22.51 |    0.16 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    69.855 μs |   1.0238 μs |  0.0561 μs |  23.89 |    0.15 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,971.924 μs | 924.8739 μs | 50.6955 μs | 674.44 |   15.58 |    4 |      - |   52.9 KB |       27.42 |
