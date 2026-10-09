```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.156 μs |   0.2895 μs |  0.0159 μs |   1.00 |    0.01 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.205 μs |   0.3938 μs |  0.0216 μs |   1.97 |    0.01 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    62.241 μs |   3.6048 μs |  0.1976 μs |  19.72 |    0.10 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    65.483 μs |   2.5601 μs |  0.1403 μs |  20.75 |    0.10 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,914.547 μs | 344.4956 μs | 18.8830 μs | 606.66 |    5.82 |    4 | 1.9531 |  52.96 KB |       27.44 |
