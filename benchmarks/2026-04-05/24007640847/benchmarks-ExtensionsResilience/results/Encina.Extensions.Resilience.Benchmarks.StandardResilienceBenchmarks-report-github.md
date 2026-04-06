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
| NoResilience_Baseline                         |  68.12 ms |    NA |  1.00 |    1 |   6.79 KB |        1.00 |
| StandardResilience_ConcurrentRequests         | 171.54 ms |    NA |  2.52 |    2 |  16.36 KB |        2.41 |
| StandardResilience_MultipleSequentialRequests | 173.46 ms |    NA |  2.55 |    3 |   14.9 KB |        2.19 |
| StandardResilience_WithRetry                  | 196.16 ms |    NA |  2.88 |    4 |  22.34 KB |        3.29 |
| StandardResilience_Success                    | 208.15 ms |    NA |  3.06 |    5 |   5.42 KB |        0.80 |
