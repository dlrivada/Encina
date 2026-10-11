```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.044 μs |  0.0471 μs |  0.0705 μs |   1.00 |    0.03 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.827 μs |  0.0383 μs |  0.0573 μs |   2.24 |    0.05 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    67.270 μs |  0.6916 μs |  1.0351 μs |  22.11 |    0.60 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    72.573 μs |  0.6751 μs |  1.0104 μs |  23.86 |    0.63 |    4 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,939.297 μs | 36.4906 μs | 54.6174 μs | 637.51 |   22.78 |    5 | 1.9531 |  52.96 KB |       27.45 |
