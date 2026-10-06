```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.190 μs |     0.1605 μs |  0.0088 μs |   1.00 |    0.00 |    1 | 0.0229 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     4.747 μs |     0.2887 μs |  0.0158 μs |   2.17 |    0.01 |    2 | 0.0229 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    46.941 μs |     0.6868 μs |  0.0376 μs |  21.43 |    0.08 |    3 | 0.2441 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    48.894 μs |     0.1220 μs |  0.0067 μs |  22.33 |    0.08 |    3 | 0.2441 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,969.353 μs | 1,400.3116 μs | 76.7558 μs | 899.23 |   30.51 |    4 |      - |  75.84 KB |       39.30 |
