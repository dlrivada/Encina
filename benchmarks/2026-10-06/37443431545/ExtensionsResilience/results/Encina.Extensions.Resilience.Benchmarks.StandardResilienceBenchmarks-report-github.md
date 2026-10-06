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
| NoResilience_Baseline                         |     2.963 μs |   0.0974 μs |  0.0053 μs |   1.00 |    0.00 |    1 | 0.1144 |   1.92 KB |        1.00 |
| StandardResilience_Success                    |     6.572 μs |   0.2750 μs |  0.0151 μs |   2.22 |    0.01 |    2 | 0.1297 |   2.24 KB |        1.17 |
| StandardResilience_MultipleSequentialRequests |    66.997 μs |   0.3120 μs |  0.0171 μs |  22.61 |    0.04 |    3 | 1.2207 |  21.32 KB |       11.09 |
| StandardResilience_ConcurrentRequests         |    68.382 μs |   9.1868 μs |  0.5036 μs |  23.08 |    0.15 |    3 | 1.3428 |  22.78 KB |       11.85 |
| StandardResilience_WithRetry                  | 2,023.844 μs | 289.5661 μs | 15.8721 μs | 683.01 |    4.76 |    4 | 3.9063 |  75.54 KB |       39.30 |
