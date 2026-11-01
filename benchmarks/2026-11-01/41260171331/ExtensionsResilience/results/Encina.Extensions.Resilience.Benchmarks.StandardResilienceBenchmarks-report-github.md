```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.879 μs |  0.0298 μs |  0.0436 μs |   1.00 |    0.03 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     3.701 μs |  0.0468 μs |  0.0656 μs |   1.97 |    0.06 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    36.245 μs |  0.8061 μs |  1.1816 μs |  19.29 |    0.76 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    37.597 μs |  0.5336 μs |  0.7821 μs |  20.01 |    0.61 |    3 | 1.2817 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,721.883 μs | 31.4252 μs | 47.0358 μs | 916.62 |   32.11 |    4 | 1.9531 |  52.62 KB |       28.78 |
