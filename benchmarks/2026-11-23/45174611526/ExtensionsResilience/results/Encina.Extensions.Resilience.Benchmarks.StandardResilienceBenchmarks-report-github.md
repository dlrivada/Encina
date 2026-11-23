```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev      | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|------------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.654 μs |     0.2314 μs |   0.0127 μs |     1.00 |    0.01 |    1 | 0.0210 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.659 μs |     0.3678 μs |   0.0202 μs |     2.21 |    0.02 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    36.810 μs |     1.5259 μs |   0.0836 μs |    22.25 |    0.15 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    37.174 μs |     5.5231 μs |   0.3027 μs |    22.47 |    0.22 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,873.470 μs | 3,040.9907 μs | 166.6870 μs | 1,132.52 |   87.59 |    4 |      - |  74.79 KB |       40.91 |
