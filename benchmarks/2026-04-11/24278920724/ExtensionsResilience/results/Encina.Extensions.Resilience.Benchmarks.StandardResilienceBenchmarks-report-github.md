```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.060 μs |  0.0125 μs |  0.0176 μs |   1.00 |    0.01 |    1 | 0.0496 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.829 μs |  0.0306 μs |  0.0448 μs |   2.83 |    0.03 |    2 | 0.0610 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    55.918 μs |  0.1214 μs |  0.1741 μs |  27.15 |    0.24 |    3 | 0.5493 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    59.067 μs |  0.5123 μs |  0.7509 μs |  28.68 |    0.43 |    4 | 0.6104 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,750.777 μs | 33.2431 μs | 49.7567 μs | 849.98 |   24.81 |    5 |      - |   6.55 KB |        5.21 |
