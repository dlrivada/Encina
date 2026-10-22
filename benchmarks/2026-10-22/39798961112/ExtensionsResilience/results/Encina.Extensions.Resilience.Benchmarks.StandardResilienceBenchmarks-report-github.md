```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Median       | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.175 μs |  0.0704 μs |  0.1032 μs |     3.231 μs |   1.00 |    0.05 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.960 μs |  0.0183 μs |  0.0269 μs |     6.958 μs |   2.19 |    0.07 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    67.833 μs |  0.9066 μs |  1.2409 μs |    68.663 μs |  21.39 |    0.78 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    70.668 μs |  0.5152 μs |  0.7712 μs |    70.768 μs |  22.28 |    0.75 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,890.203 μs | 26.9618 μs | 40.3552 μs | 1,893.227 μs | 595.93 |   22.80 |    4 | 3.9063 |  75.17 KB |       41.12 |
