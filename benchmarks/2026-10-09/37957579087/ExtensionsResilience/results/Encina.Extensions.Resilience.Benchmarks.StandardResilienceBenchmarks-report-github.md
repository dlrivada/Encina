```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.810 μs |   0.1280 μs |  0.0070 μs |   1.00 |    0.00 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     3.601 μs |   2.8726 μs |  0.1575 μs |   1.99 |    0.08 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    37.077 μs |  19.5684 μs |  1.0726 μs |  20.49 |    0.52 |    3 | 1.2817 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    38.979 μs |  39.4789 μs |  2.1640 μs |  21.54 |    1.04 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,772.832 μs | 689.1625 μs | 37.7753 μs | 979.60 |   18.37 |    4 | 1.9531 |  53.02 KB |       27.47 |
