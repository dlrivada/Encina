```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.008 μs |   0.2929 μs |  0.0161 μs |   1.00 |    0.01 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     6.491 μs |   0.1984 μs |  0.0109 μs |   2.16 |    0.01 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    65.850 μs |   3.1037 μs |  0.1701 μs |  21.89 |    0.11 |    3 | 1.2207 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    68.061 μs |   4.9223 μs |  0.2698 μs |  22.63 |    0.13 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,953.027 μs | 363.2784 μs | 19.9125 μs | 649.37 |    6.47 |    4 |      - |  52.85 KB |       27.39 |
