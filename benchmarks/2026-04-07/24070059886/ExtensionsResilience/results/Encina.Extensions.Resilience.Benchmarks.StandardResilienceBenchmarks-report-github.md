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
| NoResilience_Baseline                         |  68.94 ms |    NA |  1.00 |    1 |   7.02 KB |        1.00 |
| StandardResilience_MultipleSequentialRequests | 172.02 ms |    NA |  2.50 |    2 |   14.9 KB |        2.12 |
| StandardResilience_ConcurrentRequests         | 176.08 ms |    NA |  2.55 |    3 |  16.36 KB |        2.33 |
| StandardResilience_Success                    | 182.06 ms |    NA |  2.64 |    4 |   5.42 KB |        0.77 |
| StandardResilience_WithRetry                  | 188.26 ms |    NA |  2.73 |    5 |  46.49 KB |        6.63 |
