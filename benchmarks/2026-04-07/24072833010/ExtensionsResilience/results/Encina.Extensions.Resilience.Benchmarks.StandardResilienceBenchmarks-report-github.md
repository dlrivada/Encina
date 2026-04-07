```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.81GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                        | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|---------------------------------------------- |----------:|------:|------:|-----:|----------:|------------:|
| NoResilience_Baseline                         |  68.91 ms |    NA |  1.00 |    1 |   6.95 KB |        1.00 |
| StandardResilience_MultipleSequentialRequests | 172.07 ms |    NA |  2.50 |    2 |   14.9 KB |        2.15 |
| StandardResilience_ConcurrentRequests         | 173.10 ms |    NA |  2.51 |    3 |  16.36 KB |        2.36 |
| StandardResilience_Success                    | 191.49 ms |    NA |  2.78 |    4 |   5.42 KB |        0.78 |
| StandardResilience_WithRetry                  | 191.95 ms |    NA |  2.79 |    5 |  18.31 KB |        2.64 |
