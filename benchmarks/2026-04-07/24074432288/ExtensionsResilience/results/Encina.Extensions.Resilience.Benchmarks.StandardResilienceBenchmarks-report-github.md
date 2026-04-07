```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.652 μs |  0.0246 μs |  0.0368 μs |   1.00 |    0.02 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.605 μs |  0.0121 μs |  0.0178 μs |   2.11 |    0.03 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    55.113 μs |  0.4741 μs |  0.6949 μs |  20.78 |    0.38 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    57.104 μs |  0.0558 μs |  0.0745 μs |  21.53 |    0.30 |    4 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,752.596 μs | 28.9379 μs | 43.3128 μs | 660.93 |   18.43 |    5 |      - |   6.64 KB |        5.28 |
