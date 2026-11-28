```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.758 μs |   0.0533 μs |  0.0029 μs |     1.00 |    0.00 |    1 | 0.0210 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.752 μs |   0.2383 μs |  0.0131 μs |     2.13 |    0.01 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    36.713 μs |   0.4695 μs |  0.0257 μs |    20.88 |    0.03 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    37.862 μs |   2.4659 μs |  0.1352 μs |    21.53 |    0.07 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,829.855 μs | 263.2594 μs | 14.4301 μs | 1,040.60 |    7.26 |    4 |      - |  52.51 KB |       28.73 |
