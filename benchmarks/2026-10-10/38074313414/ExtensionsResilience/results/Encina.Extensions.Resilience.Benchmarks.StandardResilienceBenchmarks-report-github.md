```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.088 μs |   0.3661 μs |  0.0201 μs |   1.00 |    0.01 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.638 μs |   0.3891 μs |  0.0213 μs |   2.15 |    0.01 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_ConcurrentRequests         |    68.317 μs |   1.6634 μs |  0.0912 μs |  22.13 |    0.13 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_MultipleSequentialRequests |    68.864 μs |  12.3916 μs |  0.6792 μs |  22.30 |    0.23 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_WithRetry                  | 1,931.444 μs | 827.3021 μs | 45.3472 μs | 625.57 |   13.20 |    4 |      - |  52.99 KB |       27.46 |
