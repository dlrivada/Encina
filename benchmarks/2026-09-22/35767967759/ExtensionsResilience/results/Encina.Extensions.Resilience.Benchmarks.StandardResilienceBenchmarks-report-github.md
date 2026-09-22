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
| NoResilience_Baseline                         |     2.361 μs |   0.0678 μs |  0.0037 μs |   1.00 |    0.00 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.752 μs |   0.0905 μs |  0.0050 μs |   2.44 |    0.00 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    57.873 μs |   3.0911 μs |  0.1694 μs |  24.51 |    0.07 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    58.339 μs |   1.6896 μs |  0.0926 μs |  24.71 |    0.05 |    3 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,748.080 μs | 649.0425 μs | 35.5762 μs | 740.29 |   13.09 |    4 |      - |   6.49 KB |        5.16 |
