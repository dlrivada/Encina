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
| NoResilience_Baseline                         |  74.20 ms |    NA |  1.00 |    1 |   7.38 KB |        1.00 |
| StandardResilience_ConcurrentRequests         | 178.50 ms |    NA |  2.41 |    2 |  16.36 KB |        2.22 |
| StandardResilience_MultipleSequentialRequests | 182.05 ms |    NA |  2.45 |    3 |   14.9 KB |        2.02 |
| StandardResilience_WithRetry                  | 184.32 ms |    NA |  2.48 |    4 |  14.48 KB |        1.96 |
| StandardResilience_Success                    | 187.34 ms |    NA |  2.52 |    5 |  29.47 KB |        4.00 |
