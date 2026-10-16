```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.343 μs |     0.3353 μs |  0.0184 μs |   1.00 |    0.01 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.440 μs |     0.4391 μs |  0.0241 μs |   1.93 |    0.01 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    64.901 μs |     6.0077 μs |  0.3293 μs |  19.42 |    0.13 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    66.008 μs |     1.9121 μs |  0.1048 μs |  19.75 |    0.10 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,901.949 μs | 1,001.7078 μs | 54.9070 μs | 568.97 |   14.48 |    4 | 1.9531 |  53.01 KB |       27.47 |
