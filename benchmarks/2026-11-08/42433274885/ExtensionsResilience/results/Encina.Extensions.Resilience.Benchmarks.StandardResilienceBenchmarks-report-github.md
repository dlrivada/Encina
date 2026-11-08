```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Median       | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.645 μs |  0.0082 μs |  0.0120 μs |     2.644 μs |   1.00 |    0.01 |    1 | 0.0725 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.642 μs |  0.1835 μs |  0.2572 μs |     6.864 μs |   2.51 |    0.10 |    2 | 0.0839 |   2.15 KB |        1.18 |
| StandardResilience_ConcurrentRequests         |    64.245 μs |  0.3272 μs |  0.4692 μs |    64.200 μs |  24.29 |    0.21 |    3 | 0.8545 |  21.84 KB |       11.95 |
| StandardResilience_MultipleSequentialRequests |    64.881 μs |  0.4513 μs |  0.6326 μs |    64.504 μs |  24.53 |    0.26 |    3 | 0.7324 |  20.38 KB |       11.15 |
| StandardResilience_WithRetry                  | 1,973.981 μs | 61.9790 μs | 92.7673 μs | 1,977.312 μs | 746.37 |   34.67 |    4 |      - |  52.53 KB |       28.73 |
