```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.903 μs |  0.0127 μs |  0.0174 μs |   1.00 |    0.01 |    1 | 0.0191 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     4.078 μs |  0.0230 μs |  0.0323 μs |   2.14 |    0.03 |    2 | 0.0229 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    40.163 μs |  0.1447 μs |  0.1981 μs |  21.11 |    0.21 |    3 | 0.2441 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    41.460 μs |  0.1841 μs |  0.2581 μs |  21.79 |    0.24 |    4 | 0.2441 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,808.079 μs | 29.2810 μs | 42.9197 μs | 950.44 |   23.75 |    5 |      - |  52.61 KB |       28.78 |
