```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                        | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|---------------------------------------------- |----------:|------:|------:|-----:|----------:|------------:|
| NoResilience_Baseline                         |  69.72 ms |    NA |  1.00 |    1 |   6.79 KB |        1.00 |
| StandardResilience_Success                    | 174.83 ms |    NA |  2.51 |    2 |   5.42 KB |        0.80 |
| StandardResilience_MultipleSequentialRequests | 174.93 ms |    NA |  2.51 |    3 |   14.9 KB |        2.19 |
| StandardResilience_ConcurrentRequests         | 176.79 ms |    NA |  2.54 |    4 |  16.36 KB |        2.41 |
| StandardResilience_WithRetry                  | 185.96 ms |    NA |  2.67 |    5 |  46.75 KB |        6.89 |
