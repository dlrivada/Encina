```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.799 μs |   0.2622 μs |  0.0144 μs |     1.00 |    0.01 |    1 | 0.0229 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     3.953 μs |   0.6565 μs |  0.0360 μs |     2.20 |    0.02 |    2 | 0.0229 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    39.612 μs |   4.0726 μs |  0.2232 μs |    22.02 |    0.19 |    3 | 0.2441 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    41.004 μs |   7.5964 μs |  0.4164 μs |    22.79 |    0.25 |    3 | 0.2441 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,839.821 μs | 825.2617 μs | 45.2354 μs | 1,022.74 |   22.90 |    4 |      - |     53 KB |       27.47 |
