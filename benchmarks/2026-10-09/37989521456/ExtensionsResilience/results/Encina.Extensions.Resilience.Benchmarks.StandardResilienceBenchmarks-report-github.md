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
| NoResilience_Baseline                         |     1.688 μs |   0.0015 μs |  0.0001 μs |     1.00 |    0.00 |    1 | 0.1163 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     3.515 μs |   1.0943 μs |  0.0600 μs |     2.08 |    0.03 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_ConcurrentRequests         |    35.884 μs |  11.0892 μs |  0.6078 μs |    21.26 |    0.31 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_MultipleSequentialRequests |    36.733 μs |  56.1354 μs |  3.0770 μs |    21.77 |    1.58 |    3 | 1.2817 |   21.4 KB |       11.09 |
| StandardResilience_WithRetry                  | 1,725.843 μs | 565.1518 μs | 30.9779 μs | 1,022.61 |   15.90 |    4 | 1.9531 |  52.94 KB |       27.43 |
