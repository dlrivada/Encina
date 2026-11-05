```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.157 μs |     0.3507 μs |  0.0192 μs |   1.00 |    0.01 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.976 μs |     0.6561 μs |  0.0360 μs |   2.21 |    0.02 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    67.653 μs |     2.2875 μs |  0.1254 μs |  21.43 |    0.12 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    71.844 μs |     9.0692 μs |  0.4971 μs |  22.76 |    0.18 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,870.880 μs | 1,405.8774 μs | 77.0609 μs | 592.65 |   21.37 |    4 |      - |   52.6 KB |       28.77 |
