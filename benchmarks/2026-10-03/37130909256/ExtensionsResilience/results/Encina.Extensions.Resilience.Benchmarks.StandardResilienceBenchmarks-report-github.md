```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio    | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|---------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     1.533 μs |   0.1666 μs |  0.0091 μs |     1.00 |    0.01 |    1 | 0.0973 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     3.268 μs |   0.1900 μs |  0.0104 μs |     2.13 |    0.01 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    33.092 μs |   0.5883 μs |  0.0322 μs |    21.59 |    0.11 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    35.635 μs |  14.8285 μs |  0.8128 μs |    23.25 |    0.47 |    3 | 1.1597 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,762.373 μs | 280.4435 μs | 15.3721 μs | 1,149.67 |   10.53 |    4 | 1.9531 |  52.65 KB |       32.88 |
