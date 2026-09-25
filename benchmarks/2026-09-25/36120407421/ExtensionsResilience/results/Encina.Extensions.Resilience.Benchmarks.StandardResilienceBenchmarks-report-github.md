```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.847 μs |   0.2475 μs |  0.0136 μs |   1.00 |    0.01 |    1 | 0.0954 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     6.045 μs |   0.1508 μs |  0.0083 μs |   2.12 |    0.01 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    58.873 μs |   1.6812 μs |  0.0922 μs |  20.68 |    0.09 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    61.544 μs |   2.4619 μs |  0.1349 μs |  21.62 |    0.10 |    3 | 1.0986 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,667.735 μs | 415.9524 μs | 22.7998 μs | 585.76 |    7.34 |    4 |      - |   6.79 KB |        4.24 |
