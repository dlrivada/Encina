```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.757 μs |   0.0439 μs |  0.0024 μs |     1.00 |    0.00 |    1 | 0.0210 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.758 μs |   0.2679 μs |  0.0147 μs |     2.14 |    0.01 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    36.790 μs |   2.2176 μs |  0.1216 μs |    20.94 |    0.06 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    38.424 μs |   5.1200 μs |  0.2806 μs |    21.87 |    0.14 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,883.771 μs | 757.3057 μs | 41.5105 μs | 1,072.00 |   20.50 |    4 |      - |  52.54 KB |       28.74 |
