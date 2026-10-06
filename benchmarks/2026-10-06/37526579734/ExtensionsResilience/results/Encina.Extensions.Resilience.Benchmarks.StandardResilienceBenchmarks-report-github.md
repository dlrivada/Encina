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
| NoResilience_Baseline                         |     2.972 μs |   0.1559 μs |  0.0085 μs |   1.00 |    0.00 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.773 μs |   1.8640 μs |  0.1022 μs |   2.28 |    0.03 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_ConcurrentRequests         |    68.007 μs |  12.7666 μs |  0.6998 μs |  22.89 |    0.21 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_MultipleSequentialRequests |    68.150 μs |  15.1489 μs |  0.8304 μs |  22.93 |    0.25 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_WithRetry                  | 1,909.614 μs | 855.5210 μs | 46.8940 μs | 642.63 |   13.76 |    4 | 1.9531 |  53.03 KB |       27.48 |
