```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.684 μs |     0.3948 μs |  0.0216 μs |     1.00 |    0.02 |    1 | 0.1163 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     3.440 μs |     0.3175 μs |  0.0174 μs |     2.04 |    0.02 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    34.009 μs |     2.4288 μs |  0.1331 μs |    20.19 |    0.23 |    3 | 1.2817 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    36.689 μs |    32.2440 μs |  1.7674 μs |    21.78 |    0.94 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,739.874 μs | 1,167.9414 μs | 64.0188 μs | 1,033.03 |   34.84 |    4 | 1.9531 |  52.94 KB |       27.43 |
