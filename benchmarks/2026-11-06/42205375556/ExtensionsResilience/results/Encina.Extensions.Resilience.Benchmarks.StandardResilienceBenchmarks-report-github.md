```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     3.265 μs |   0.2077 μs |  0.0114 μs |   1.00 |    0.00 |    1 | 0.1106 |   1.83 KB |        1.00 |
| StandardResilience_Success                    |     6.773 μs |   0.4176 μs |  0.0229 μs |   2.07 |    0.01 |    2 | 0.1297 |   2.15 KB |        1.18 |
| StandardResilience_MultipleSequentialRequests |    70.629 μs |   2.4968 μs |  0.1369 μs |  21.63 |    0.07 |    3 | 1.2207 |  20.38 KB |       11.15 |
| StandardResilience_ConcurrentRequests         |    71.914 μs |   5.0379 μs |  0.2761 μs |  22.02 |    0.10 |    3 | 1.2207 |  21.84 KB |       11.95 |
| StandardResilience_WithRetry                  | 1,947.618 μs | 911.5145 μs | 49.9632 μs | 596.49 |   13.37 |    4 | 3.9063 |  75.56 KB |       41.33 |
