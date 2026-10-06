```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.137 μs |   0.0676 μs |  0.0037 μs |   1.00 |    0.00 |    1 | 0.0229 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     4.572 μs |   0.1480 μs |  0.0081 μs |   2.14 |    0.00 |    2 | 0.0229 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    45.744 μs |   5.8909 μs |  0.3229 μs |  21.41 |    0.13 |    3 | 0.2441 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    47.441 μs |   9.5856 μs |  0.5254 μs |  22.20 |    0.22 |    3 | 0.2441 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,878.604 μs | 228.8471 μs | 12.5439 μs | 879.29 |    5.25 |    4 |      - |  52.92 KB |       27.42 |
