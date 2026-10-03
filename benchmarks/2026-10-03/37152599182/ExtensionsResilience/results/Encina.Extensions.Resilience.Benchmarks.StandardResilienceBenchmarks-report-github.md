```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.172 μs |     0.3928 μs |  0.0215 μs |   1.00 |    0.01 |    1 | 0.0954 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     4.600 μs |     0.4791 μs |  0.0263 μs |   2.12 |    0.02 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    45.990 μs |     6.0144 μs |  0.3297 μs |  21.17 |    0.23 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    47.402 μs |    12.2614 μs |  0.6721 μs |  21.82 |    0.33 |    3 | 1.1597 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,811.303 μs | 1,071.8124 μs | 58.7497 μs | 833.93 |   24.51 |    4 | 1.9531 |  52.64 KB |       32.87 |
