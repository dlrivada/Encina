```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.682 μs |   0.1779 μs |  0.0098 μs |   1.00 |    0.00 |    1 | 0.0954 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     6.347 μs |   0.4142 μs |  0.0227 μs |   2.37 |    0.01 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    63.746 μs |   2.4827 μs |  0.1361 μs |  23.77 |    0.09 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    63.898 μs |   5.6600 μs |  0.3102 μs |  23.82 |    0.13 |    3 | 1.0986 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,911.267 μs | 665.7183 μs | 36.4903 μs | 712.54 |   11.99 |    4 |      - |  52.59 KB |       32.84 |
