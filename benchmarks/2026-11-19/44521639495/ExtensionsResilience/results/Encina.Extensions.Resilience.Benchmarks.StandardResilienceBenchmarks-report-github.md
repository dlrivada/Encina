```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.292 μs |     0.0383 μs |  0.0021 μs |   1.00 |    0.00 |    1 | 0.0191 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     4.880 μs |     0.3526 μs |  0.0193 μs |   2.13 |    0.01 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    48.470 μs |     4.0553 μs |  0.2223 μs |  21.15 |    0.09 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    49.721 μs |     2.1557 μs |  0.1182 μs |  21.69 |    0.05 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,836.956 μs | 1,249.6482 μs | 68.4974 μs | 801.48 |   25.89 |    4 |      - |  52.57 KB |       28.76 |
