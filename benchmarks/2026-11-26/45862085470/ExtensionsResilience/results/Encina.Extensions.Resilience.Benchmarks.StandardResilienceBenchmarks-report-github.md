```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.735 μs |   0.0124 μs |  0.0007 μs |     1.00 |    0.00 |    1 | 0.0210 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.695 μs |   0.1058 μs |  0.0058 μs |     2.13 |    0.00 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    36.280 μs |   1.4448 μs |  0.0792 μs |    20.92 |    0.04 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    37.428 μs |   1.5316 μs |  0.0840 μs |    21.58 |    0.04 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,753.508 μs | 926.2563 μs | 50.7712 μs | 1,010.89 |   25.35 |    4 |      - |  52.51 KB |       28.72 |
