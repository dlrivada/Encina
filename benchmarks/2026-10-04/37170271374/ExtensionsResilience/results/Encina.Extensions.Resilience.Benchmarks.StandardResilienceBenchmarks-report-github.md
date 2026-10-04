```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.765 μs |  0.0212 μs |  0.0318 μs |     1.00 |    0.02 |    1 | 0.0191 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     4.033 μs |  0.0504 μs |  0.0738 μs |     2.29 |    0.06 |    2 | 0.0229 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    40.552 μs |  0.4122 μs |  0.5912 μs |    22.99 |    0.52 |    3 | 0.1831 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    41.582 μs |  0.4368 μs |  0.6537 μs |    23.57 |    0.55 |    3 | 0.1831 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,846.895 μs | 26.6690 μs | 39.9169 μs | 1,046.99 |   28.89 |    4 |      - |  52.68 KB |       32.89 |
