```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.658 μs |   0.0405 μs |  0.0022 μs |     1.00 |    0.00 |    1 | 0.1163 |   1.92 KB |        1.00 |
| StandardResilience_Success                    |     3.516 μs |   0.8359 μs |  0.0458 μs |     2.12 |    0.02 |    2 | 0.1335 |   2.24 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    36.280 μs |   0.6356 μs |  0.0348 μs |    21.88 |    0.03 |    3 | 1.2817 |  21.32 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    37.538 μs |   4.1042 μs |  0.2250 μs |    22.64 |    0.12 |    3 | 1.3428 |  22.78 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,761.734 μs | 272.6242 μs | 14.9435 μs | 1,062.73 |    7.90 |    4 | 1.9531 |  53.02 KB |       27.59 |
