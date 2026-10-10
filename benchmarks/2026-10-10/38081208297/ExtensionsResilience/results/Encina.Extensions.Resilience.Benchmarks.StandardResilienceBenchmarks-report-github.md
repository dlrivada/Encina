```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.399 μs |   0.2243 μs |  0.0123 μs |   1.00 |    0.01 |    1 | 0.1144 |   1.93 KB |        1.00 |
| StandardResilience_Success                    |     4.811 μs |   0.1709 μs |  0.0094 μs |   2.01 |    0.01 |    2 | 0.1373 |   2.25 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    47.823 μs |   0.9280 μs |  0.0509 μs |  19.94 |    0.09 |    3 | 1.2817 |   21.4 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    50.518 μs |   1.2466 μs |  0.0683 μs |  21.06 |    0.10 |    3 | 1.3428 |  22.86 KB |       11.85 |
| StandardResilience_WithRetry                  | 1,837.332 μs | 501.9315 μs | 27.5126 μs | 765.95 |   10.50 |    4 | 1.9531 |  53.07 KB |       27.50 |
