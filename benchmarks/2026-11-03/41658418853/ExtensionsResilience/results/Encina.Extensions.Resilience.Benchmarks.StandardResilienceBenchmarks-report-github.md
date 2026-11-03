```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.679 μs |   0.9444 μs |  0.0518 μs |     1.00 |    0.04 |    1 | 0.0210 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.439 μs |   0.2834 μs |  0.0155 μs |     2.05 |    0.05 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    34.173 μs |   2.6461 μs |  0.1450 μs |    20.36 |    0.54 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    38.619 μs |  58.0716 μs |  3.1831 μs |    23.01 |    1.75 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,839.225 μs | 393.1110 μs | 21.5477 μs | 1,095.95 |   30.85 |    4 |      - |  52.51 KB |       28.72 |
