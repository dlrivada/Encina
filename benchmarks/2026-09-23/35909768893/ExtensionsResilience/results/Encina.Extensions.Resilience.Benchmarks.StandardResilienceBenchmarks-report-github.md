```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.826 μs |     0.0927 μs |  0.0051 μs |   1.00 |    0.00 |    1 | 0.0954 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     6.410 μs |     0.4087 μs |  0.0224 μs |   2.27 |    0.01 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    63.365 μs |     5.0860 μs |  0.2788 μs |  22.43 |    0.09 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    64.307 μs |     2.8246 μs |  0.1548 μs |  22.76 |    0.06 |    3 | 1.0986 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,781.235 μs | 1,049.6480 μs | 57.5348 μs | 630.40 |   17.66 |    4 |      - |   6.96 KB |        4.34 |
