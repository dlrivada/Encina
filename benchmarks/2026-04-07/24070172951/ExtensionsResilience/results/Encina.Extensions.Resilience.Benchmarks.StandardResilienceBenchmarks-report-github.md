```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.036 μs |   0.1630 μs |  0.0089 μs |   1.00 |    0.01 |    1 | 0.0496 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.863 μs |   0.4653 μs |  0.0255 μs |   2.88 |    0.02 |    2 | 0.0610 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    58.032 μs |   6.5123 μs |  0.3570 μs |  28.50 |    0.19 |    3 | 0.5493 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    59.964 μs |   1.7978 μs |  0.0985 μs |  29.45 |    0.12 |    3 | 0.6104 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,697.760 μs | 330.5553 μs | 18.1189 μs | 833.88 |    8.33 |    4 |      - |   6.56 KB |        5.22 |
