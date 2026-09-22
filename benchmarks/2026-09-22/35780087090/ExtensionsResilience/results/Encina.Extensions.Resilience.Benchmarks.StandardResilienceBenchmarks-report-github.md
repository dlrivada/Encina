```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.290 μs |   0.3181 μs |  0.0174 μs |     1.00 |    0.02 |    1 | 0.0153 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     2.872 μs |   0.1318 μs |  0.0072 μs |     2.23 |    0.03 |    2 | 0.0191 |   1.58 KB |        1.25 |
| StandardResilience_ConcurrentRequests         |    29.792 μs |   0.1674 μs |  0.0092 μs |    23.10 |    0.27 |    3 | 0.1831 |  16.14 KB |       12.83 |
| StandardResilience_MultipleSequentialRequests |    31.579 μs |  44.1195 μs |  2.4183 μs |    24.49 |    1.65 |    3 | 0.1221 |  14.68 KB |       11.67 |
| StandardResilience_WithRetry                  | 1,747.646 μs | 403.0959 μs | 22.0950 μs | 1,355.35 |   21.80 |    4 |      - |   6.56 KB |        5.22 |
