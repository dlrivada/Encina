```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.25GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.588 μs |   0.0532 μs |  0.0029 μs |   1.00 |    0.00 |    1 | 0.0763 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.497 μs |   0.1504 μs |  0.0082 μs |   2.51 |    0.00 |    2 | 0.0916 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    61.152 μs |   1.9295 μs |  0.1058 μs |  23.63 |    0.04 |    3 | 0.8545 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    69.523 μs |   2.2800 μs |  0.1250 μs |  26.87 |    0.05 |    3 | 0.8545 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,946.185 μs | 417.6376 μs | 22.8921 μs | 752.14 |    7.70 |    4 |      - |  53.06 KB |       27.50 |
