```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.615 μs |     0.1340 μs |  0.0073 μs |     1.00 |    0.01 |    1 | 0.0973 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     3.433 μs |     1.1925 μs |  0.0654 μs |     2.13 |    0.04 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    38.065 μs |    54.2050 μs |  2.9712 μs |    23.57 |    1.60 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    38.239 μs |    63.2764 μs |  3.4684 μs |    23.67 |    1.86 |    3 | 1.1597 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,634.918 μs | 1,130.6777 μs | 61.9763 μs | 1,012.18 |   33.47 |    4 |      - |   6.96 KB |        4.35 |
