```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.271 μs |   0.2896 μs |  0.0159 μs |   1.00 |    0.01 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.844 μs |   0.2578 μs |  0.0141 μs |   2.09 |    0.01 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    69.568 μs |   3.9448 μs |  0.2162 μs |  21.27 |    0.11 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    72.476 μs |   3.5627 μs |  0.1953 μs |  22.16 |    0.11 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,838.995 μs | 904.5035 μs | 49.5789 μs | 562.24 |   13.34 |    4 | 1.9531 |   52.6 KB |       28.77 |
