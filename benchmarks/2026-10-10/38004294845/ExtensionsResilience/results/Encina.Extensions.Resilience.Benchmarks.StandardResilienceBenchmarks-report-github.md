```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.850 μs |     0.4589 μs |  0.0252 μs |   1.00 |    0.01 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.132 μs |     1.1269 μs |  0.0618 μs |   2.15 |    0.02 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    59.261 μs |     6.4261 μs |  0.3522 μs |  20.79 |    0.19 |    3 | 1.2817 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    64.097 μs |     8.6228 μs |  0.4726 μs |  22.49 |    0.22 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,931.271 μs | 1,056.3459 μs | 57.9019 μs | 677.62 |   18.34 |    4 | 1.9531 |     53 KB |       27.47 |
