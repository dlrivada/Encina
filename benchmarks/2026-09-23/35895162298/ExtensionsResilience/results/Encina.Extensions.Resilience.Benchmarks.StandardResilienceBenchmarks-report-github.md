```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.394 μs |   0.1243 μs |  0.0068 μs |     1.00 |    0.01 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     3.209 μs |   2.5477 μs |  0.1396 μs |     2.30 |    0.09 |    2 | 0.0954 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    31.154 μs |   1.2425 μs |  0.0681 μs |    22.35 |    0.10 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    31.965 μs |   1.7667 μs |  0.0968 μs |    22.93 |    0.11 |    3 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,743.794 μs | 429.2869 μs | 23.5307 μs | 1,250.72 |   15.54 |    4 |      - |   6.63 KB |        5.27 |
