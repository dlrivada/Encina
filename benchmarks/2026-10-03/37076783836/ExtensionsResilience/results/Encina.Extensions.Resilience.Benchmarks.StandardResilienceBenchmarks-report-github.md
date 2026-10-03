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
| NoResilience_Baseline                         |     1.983 μs |   0.2601 μs |  0.0143 μs |     1.00 |    0.01 |    1 | 0.0191 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     4.644 μs |   0.6465 μs |  0.0354 μs |     2.34 |    0.02 |    2 | 0.0229 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    45.036 μs |   4.2297 μs |  0.2318 μs |    22.72 |    0.17 |    3 | 0.1831 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    45.956 μs |   6.2424 μs |  0.3422 μs |    23.18 |    0.21 |    3 | 0.1831 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 2,002.592 μs | 703.8669 μs | 38.5813 μs | 1,010.13 |   18.00 |    4 |      - |  75.18 KB |       46.94 |
