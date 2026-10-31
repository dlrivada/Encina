```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.245 μs |  0.0907 μs | 0.0050 μs |   1.00 |    0.00 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     7.004 μs |  0.2422 μs | 0.0133 μs |   2.16 |    0.00 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    69.694 μs |  6.3412 μs | 0.3476 μs |  21.48 |    0.10 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    73.963 μs |  3.2481 μs | 0.1780 μs |  22.79 |    0.06 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,968.244 μs | 90.6548 μs | 4.9691 μs | 606.54 |    1.55 |    4 | 3.9063 |  75.44 KB |       41.26 |
