```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.824 μs |   0.2230 μs |  0.0122 μs |   1.00 |    0.01 |    1 | 0.0954 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     5.982 μs |   0.3644 μs |  0.0200 μs |   2.12 |    0.01 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    59.744 μs |   6.9509 μs |  0.3810 μs |  21.16 |    0.14 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    61.224 μs |   2.7239 μs |  0.1493 μs |  21.68 |    0.09 |    3 | 1.0986 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,729.676 μs | 977.1067 μs | 53.5585 μs | 612.58 |   16.59 |    4 |      - |   6.88 KB |        4.29 |
