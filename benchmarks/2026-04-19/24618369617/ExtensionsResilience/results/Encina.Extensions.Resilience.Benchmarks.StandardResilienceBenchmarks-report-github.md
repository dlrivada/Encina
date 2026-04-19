```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Median       | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.355 μs |  0.0527 μs |  0.0722 μs |     2.298 μs |   1.00 |    0.04 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.706 μs |  0.0913 μs |  0.1339 μs |     5.619 μs |   2.43 |    0.09 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    57.437 μs |  0.6360 μs |  0.9519 μs |    57.468 μs |  24.41 |    0.83 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    59.710 μs |  0.2157 μs |  0.3023 μs |    59.891 μs |  25.38 |    0.77 |    4 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,768.367 μs | 30.0531 μs | 44.0515 μs | 1,761.889 μs | 751.65 |   29.09 |    5 |      - |   6.62 KB |        5.26 |
