```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                        | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|---------------------------------------------- |----------:|------:|------:|-----:|----------:|------------:|
| NoResilience_Baseline                         |  74.86 ms |    NA |  1.00 |    1 |   6.79 KB |        1.00 |
| StandardResilience_MultipleSequentialRequests | 172.67 ms |    NA |  2.31 |    2 |   14.9 KB |        2.19 |
| StandardResilience_ConcurrentRequests         | 175.49 ms |    NA |  2.34 |    3 |  16.36 KB |        2.41 |
| StandardResilience_WithRetry                  | 182.83 ms |    NA |  2.44 |    4 |  45.41 KB |        6.69 |
| StandardResilience_Success                    | 213.19 ms |    NA |  2.85 |    5 |   5.42 KB |        0.80 |
