```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.243 μs |   0.0399 μs |  0.0022 μs |   1.00 |    0.00 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     7.231 μs |   0.5492 μs |  0.0301 μs |   2.23 |    0.01 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    68.275 μs |   5.4842 μs |  0.3006 μs |  21.05 |    0.08 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    69.751 μs |   4.5762 μs |  0.2508 μs |  21.51 |    0.07 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,955.443 μs | 596.0082 μs | 32.6692 μs | 603.02 |    8.73 |    4 |      - |  53.03 KB |       27.48 |
