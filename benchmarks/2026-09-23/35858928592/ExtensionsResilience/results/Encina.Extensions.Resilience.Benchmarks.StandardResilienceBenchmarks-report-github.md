```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.581 μs |   0.2015 μs |  0.0110 μs |   1.00 |    0.01 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.522 μs |   0.0627 μs |  0.0034 μs |   2.14 |    0.01 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    54.104 μs |   1.1248 μs |  0.0617 μs |  20.96 |    0.08 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    57.248 μs |   1.8588 μs |  0.1019 μs |  22.18 |    0.09 |    3 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,729.171 μs | 818.9746 μs | 44.8908 μs | 670.02 |   15.27 |    4 |      - |   6.55 KB |        5.21 |
