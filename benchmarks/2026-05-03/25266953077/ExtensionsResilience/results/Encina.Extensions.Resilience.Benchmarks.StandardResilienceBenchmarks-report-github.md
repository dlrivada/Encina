```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.472 μs |  0.0212 μs |  0.0317 μs |   1.00 |    0.02 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.855 μs |  0.0278 μs |  0.0381 μs |   2.37 |    0.03 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    56.695 μs |  0.1424 μs |  0.2088 μs |  22.94 |    0.30 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    62.042 μs |  1.8337 μs |  2.6298 μs |  25.10 |    1.09 |    4 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,778.604 μs | 32.0293 μs | 47.9400 μs | 719.57 |   21.14 |    5 |      - |   6.54 KB |        5.20 |
