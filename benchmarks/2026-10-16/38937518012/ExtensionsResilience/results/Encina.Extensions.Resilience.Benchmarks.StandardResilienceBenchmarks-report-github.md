```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.214 μs |   0.0257 μs |  0.0014 μs |   1.00 |    0.00 |    1 | 0.0191 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     4.799 μs |   0.1199 μs |  0.0066 μs |   2.17 |    0.00 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    46.922 μs |   2.4514 μs |  0.1344 μs |  21.19 |    0.05 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    49.407 μs |   6.0533 μs |  0.3318 μs |  22.32 |    0.13 |    3 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,842.964 μs | 520.0209 μs | 28.5041 μs | 832.47 |   11.16 |    4 |      - |  52.56 KB |       28.75 |
