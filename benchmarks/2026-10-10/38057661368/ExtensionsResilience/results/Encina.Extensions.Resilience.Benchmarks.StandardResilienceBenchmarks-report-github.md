```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.738 μs |     0.1887 μs |  0.0103 μs |     1.00 |    0.01 |    1 | 0.0229 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     3.356 μs |     0.3238 μs |  0.0178 μs |     1.93 |    0.01 |    2 | 0.0267 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    33.382 μs |     5.3040 μs |  0.2907 μs |    19.20 |    0.18 |    3 | 0.2441 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    34.094 μs |     0.8141 μs |  0.0446 μs |    19.61 |    0.10 |    3 | 0.2441 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,876.297 μs | 1,235.2786 μs | 67.7098 μs | 1,079.38 |   34.19 |    4 |      - |  76.07 KB |       39.42 |
