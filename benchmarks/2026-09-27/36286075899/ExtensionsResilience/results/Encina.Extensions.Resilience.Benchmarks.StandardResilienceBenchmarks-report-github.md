```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.402 μs |  0.0350 μs |  0.0502 μs |     1.00 |    0.05 |    1 | 0.0191 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     3.244 μs |  0.0930 μs |  0.1334 μs |     2.32 |    0.12 |    2 | 0.0229 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    32.071 μs |  0.8303 μs |  1.1908 μs |    22.90 |    1.13 |    3 | 0.1831 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    32.660 μs |  0.2466 μs |  0.3691 μs |    23.33 |    0.82 |    3 | 0.1831 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,741.676 μs | 31.1774 μs | 45.6994 μs | 1,243.91 |   52.40 |    4 |      - |   6.78 KB |        4.23 |
