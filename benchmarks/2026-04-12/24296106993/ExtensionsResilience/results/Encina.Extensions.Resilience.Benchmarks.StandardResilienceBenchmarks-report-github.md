```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Median       | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.432 μs |  0.0281 μs |  0.0411 μs |     2.412 μs |   1.00 |    0.02 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.826 μs |  0.0629 μs |  0.0942 μs |     5.835 μs |   2.40 |    0.06 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    59.747 μs |  1.5707 μs |  2.3023 μs |    57.768 μs |  24.58 |    1.02 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    61.106 μs |  0.1141 μs |  0.1672 μs |    61.132 μs |  25.14 |    0.42 |    3 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,760.941 μs | 25.1990 μs | 37.7166 μs | 1,761.158 μs | 724.35 |   19.44 |    4 |      - |   6.55 KB |        5.21 |
