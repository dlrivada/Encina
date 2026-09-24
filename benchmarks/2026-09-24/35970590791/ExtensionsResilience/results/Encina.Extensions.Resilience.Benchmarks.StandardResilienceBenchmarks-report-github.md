```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.714 μs |   0.6102 μs |  0.0334 μs |   1.00 |    0.02 |    1 | 0.0973 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     3.659 μs |   0.2434 μs |  0.0133 μs |   2.14 |    0.04 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    36.432 μs |  27.4709 μs |  1.5058 μs |  21.26 |    0.84 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    36.654 μs |   3.3432 μs |  0.1833 μs |  21.39 |    0.37 |    3 | 1.1597 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,654.154 μs | 440.5042 μs | 24.1455 μs | 965.45 |   20.31 |    4 |      - |   6.87 KB |        4.29 |
