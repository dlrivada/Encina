```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.794 μs |   0.8835 μs |  0.0484 μs |   1.00 |    0.03 |    1 | 0.1163 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     3.651 μs |   0.2939 μs |  0.0161 μs |   2.04 |    0.05 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    35.915 μs |   5.3632 μs |  0.2940 μs |  20.03 |    0.49 |    3 | 1.2817 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    37.795 μs |   5.2734 μs |  0.2891 μs |  21.08 |    0.52 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,690.786 μs | 675.6562 μs | 37.0350 μs | 942.94 |   28.50 |    4 | 1.9531 |  52.93 KB |       27.43 |
