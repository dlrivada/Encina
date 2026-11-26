```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.178 μs |   0.6533 μs |  0.0358 μs |   1.00 |    0.01 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     7.000 μs |   0.9252 μs |  0.0507 μs |   2.20 |    0.03 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    67.340 μs |   1.0306 μs |  0.0565 μs |  21.19 |    0.21 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    72.258 μs |   6.5433 μs |  0.3587 μs |  22.74 |    0.24 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,885.609 μs | 500.6600 μs | 27.4429 μs | 593.30 |    9.47 |    4 | 1.9531 |  52.59 KB |       28.77 |
