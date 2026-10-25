```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Median       | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.130 μs |  0.0587 μs |  0.0822 μs |     3.062 μs |   1.00 |    0.04 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.997 μs |  0.0216 μs |  0.0310 μs |     7.000 μs |   2.24 |    0.06 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    68.797 μs |  0.7127 μs |  1.0447 μs |    68.125 μs |  21.99 |    0.65 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    72.316 μs |  0.4651 μs |  0.6670 μs |    71.967 μs |  23.12 |    0.63 |    4 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,936.956 μs | 34.5228 μs | 51.6721 μs | 1,925.050 μs | 619.26 |   22.73 |    5 |      - |  52.58 KB |       28.76 |
