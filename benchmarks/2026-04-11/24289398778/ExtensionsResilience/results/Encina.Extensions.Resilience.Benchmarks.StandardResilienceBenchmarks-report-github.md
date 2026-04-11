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
| NoResilience_Baseline                         |     2.646 μs |  0.0059 μs |  0.0081 μs |   1.00 |    0.00 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.656 μs |  0.0186 μs |  0.0279 μs |   2.14 |    0.01 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    54.830 μs |  0.1111 μs |  0.1520 μs |  20.72 |    0.08 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    57.867 μs |  0.3888 μs |  0.5322 μs |  21.87 |    0.21 |    4 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,786.076 μs | 34.2911 μs | 51.3253 μs | 674.91 |   19.19 |    5 |      - |   6.46 KB |        5.14 |
