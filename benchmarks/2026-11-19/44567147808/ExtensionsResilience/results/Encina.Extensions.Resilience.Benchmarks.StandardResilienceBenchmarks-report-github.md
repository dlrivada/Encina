```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.253 μs |   0.0349 μs |  0.0019 μs |   1.00 |    0.00 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     7.078 μs |   0.6129 μs |  0.0336 μs |   2.18 |    0.01 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    70.608 μs |   6.2396 μs |  0.3420 μs |  21.70 |    0.09 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    74.215 μs |   8.3488 μs |  0.4576 μs |  22.81 |    0.12 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,864.565 μs | 563.4425 μs | 30.8842 μs | 573.12 |    8.23 |    4 | 1.9531 |  52.65 KB |       28.80 |
