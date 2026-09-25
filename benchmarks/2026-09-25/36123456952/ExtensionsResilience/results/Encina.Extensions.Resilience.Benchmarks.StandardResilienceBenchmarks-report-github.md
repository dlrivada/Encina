```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                        | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.695 μs |     0.0857 μs |  0.0047 μs |   1.00 |    0.00 |    1 | 0.0954 |    1.6 KB |        1.00 |
| StandardResilience_Success                    |     6.166 μs |     0.0183 μs |  0.0010 μs |   2.29 |    0.00 |    2 | 0.1144 |   1.92 KB |        1.20 |
| StandardResilience_MultipleSequentialRequests |    62.477 μs |     2.5716 μs |  0.1410 μs |  23.18 |    0.06 |    3 | 1.0986 |  18.12 KB |       11.31 |
| StandardResilience_ConcurrentRequests         |    64.471 μs |     3.2567 μs |  0.1785 μs |  23.92 |    0.07 |    3 | 1.0986 |  19.58 KB |       12.22 |
| StandardResilience_WithRetry                  | 1,749.058 μs | 1,042.8045 μs | 57.1596 μs | 648.97 |   18.39 |    4 |      - |   6.78 KB |        4.23 |
