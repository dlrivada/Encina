```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.837 μs |   0.4651 μs |  0.0255 μs |   1.00 |    0.01 |    1 | 0.0954 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     6.181 μs |   0.1354 μs |  0.0074 μs |   2.18 |    0.02 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    62.009 μs |   0.7992 μs |  0.0438 μs |  21.86 |    0.17 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    65.474 μs |   0.3645 μs |  0.0200 μs |  23.08 |    0.18 |    3 | 1.0986 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,937.630 μs | 234.4612 μs | 12.8516 μs | 683.11 |    6.63 |    4 | 1.9531 |  52.59 KB |       32.84 |
