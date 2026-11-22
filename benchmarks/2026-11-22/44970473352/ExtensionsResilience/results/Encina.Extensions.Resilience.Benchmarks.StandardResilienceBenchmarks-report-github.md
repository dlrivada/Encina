```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.274 μs |  0.0058 μs |  0.0087 μs |   1.00 |    0.01 |    1 | 0.0191 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     4.925 μs |  0.0158 μs |  0.0226 μs |   2.17 |    0.01 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    48.540 μs |  0.1430 μs |  0.2141 μs |  21.35 |    0.12 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    49.837 μs |  0.3607 μs |  0.5056 μs |  21.92 |    0.23 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,876.393 μs | 30.9222 μs | 46.2829 μs | 825.26 |   20.26 |    4 |      - |  75.29 KB |       41.18 |
