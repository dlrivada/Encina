```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.762 μs |     0.1004 μs |  0.0055 μs |     1.00 |    0.00 |    1 | 0.0210 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.734 μs |     0.1485 μs |  0.0081 μs |     2.12 |    0.01 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    36.559 μs |     0.3499 μs |  0.0192 μs |    20.75 |    0.06 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    38.143 μs |     2.1515 μs |  0.1179 μs |    21.65 |    0.08 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,783.732 μs | 1,283.3142 μs | 70.3428 μs | 1,012.26 |   34.68 |    4 |      - |  52.58 KB |       28.76 |
