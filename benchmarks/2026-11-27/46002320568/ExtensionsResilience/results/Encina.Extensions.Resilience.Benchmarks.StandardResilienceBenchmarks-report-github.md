```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.307 μs |   0.2840 μs |  0.0156 μs |   1.00 |    0.01 |    1 | 0.0191 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     4.981 μs |   0.7010 μs |  0.0384 μs |   2.16 |    0.02 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    48.932 μs |   3.7641 μs |  0.2063 μs |  21.21 |    0.15 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    50.440 μs |   2.9143 μs |  0.1597 μs |  21.87 |    0.14 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,940.925 μs | 410.3628 μs | 22.4934 μs | 841.39 |    9.78 |    4 |      - |  75.08 KB |       41.07 |
