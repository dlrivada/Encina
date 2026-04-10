```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.362 μs |   0.1200 μs |  0.0066 μs |   1.00 |    0.00 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.902 μs |   0.3445 μs |  0.0189 μs |   2.50 |    0.01 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    56.685 μs |   2.1988 μs |  0.1205 μs |  24.00 |    0.07 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    58.415 μs |   1.9600 μs |  0.1074 μs |  24.74 |    0.07 |    3 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,762.248 μs | 218.1043 μs | 11.9550 μs | 746.23 |    4.74 |    4 |      - |   6.54 KB |        5.20 |
