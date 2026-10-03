```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.785 μs |   0.1991 μs |  0.0109 μs |   1.00 |    0.00 |    1 | 0.0954 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     5.940 μs |   0.4245 μs |  0.0233 μs |   2.13 |    0.01 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    58.406 μs |   0.8846 μs |  0.0485 μs |  20.97 |    0.07 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    60.794 μs |   2.2950 μs |  0.1258 μs |  21.83 |    0.08 |    3 | 1.1597 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,867.742 μs | 968.8623 μs | 53.1066 μs | 670.61 |   16.67 |    4 | 3.9063 |  75.41 KB |       47.09 |
