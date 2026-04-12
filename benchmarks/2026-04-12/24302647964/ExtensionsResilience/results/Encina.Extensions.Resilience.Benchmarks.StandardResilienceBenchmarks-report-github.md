```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                        | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| NoResilience_Baseline                         |     2.601 μs |  0.0070 μs |  0.0096 μs |   1.00 |    0.01 |    1 | 0.0763 |   1.26 KB |        1.00 |
| StandardResilience_Success                    |     5.611 μs |  0.0096 μs |  0.0143 μs |   2.16 |    0.01 |    2 | 0.0916 |   1.58 KB |        1.25 |
| StandardResilience_MultipleSequentialRequests |    55.062 μs |  0.2053 μs |  0.3009 μs |  21.17 |    0.14 |    3 | 0.8545 |  14.68 KB |       11.67 |
| StandardResilience_ConcurrentRequests         |    57.510 μs |  0.2892 μs |  0.4147 μs |  22.11 |    0.18 |    4 | 0.9766 |  16.14 KB |       12.83 |
| StandardResilience_WithRetry                  | 1,736.222 μs | 33.9394 μs | 50.7988 μs | 667.45 |   19.36 |    5 |      - |   6.58 KB |        5.23 |
