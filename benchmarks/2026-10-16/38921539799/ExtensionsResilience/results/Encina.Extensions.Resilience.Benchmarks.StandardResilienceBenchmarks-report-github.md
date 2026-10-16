```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.654 μs |   0.1073 μs |  0.0059 μs |     1.00 |    0.00 |    1 | 0.0210 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.421 μs |   0.0616 μs |  0.0034 μs |     2.07 |    0.01 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    34.025 μs |   1.0065 μs |  0.0552 μs |    20.57 |    0.07 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    40.165 μs |  33.3175 μs |  1.8262 μs |    24.28 |    0.96 |    4 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,849.422 μs | 806.6532 μs | 44.2154 μs | 1,118.21 |   23.41 |    5 |      - |   52.5 KB |       28.72 |
