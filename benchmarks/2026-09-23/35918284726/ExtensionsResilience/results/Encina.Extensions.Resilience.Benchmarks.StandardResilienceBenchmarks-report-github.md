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
| NoResilience_Baseline                         |     2.646 μs |   0.0752 μs |  0.0041 μs |   1.00 |    0.00 |    1 | 0.0954 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     6.110 μs |   0.2395 μs |  0.0131 μs |   2.31 |    0.01 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    60.537 μs |   2.6352 μs |  0.1444 μs |  22.88 |    0.06 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    63.028 μs |   2.8406 μs |  0.1557 μs |  23.82 |    0.06 |    3 | 1.0986 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,709.086 μs | 444.9916 μs | 24.3915 μs | 645.85 |    8.03 |    4 |      - |   6.95 KB |        4.34 |
