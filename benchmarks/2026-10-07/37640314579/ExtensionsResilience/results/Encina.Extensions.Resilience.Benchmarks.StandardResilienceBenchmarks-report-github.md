```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.829 μs |     1.0953 μs |  0.0600 μs |   1.00 |    0.03 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.937 μs |     0.2532 μs |  0.0139 μs |   2.45 |    0.04 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    66.053 μs |     3.6840 μs |  0.2019 μs |  23.35 |    0.43 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    68.753 μs |     2.8068 μs |  0.1539 μs |  24.31 |    0.45 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,933.707 μs | 1,645.7749 μs | 90.2105 μs | 683.69 |   30.30 |    4 |      - |   53.1 KB |       27.52 |
