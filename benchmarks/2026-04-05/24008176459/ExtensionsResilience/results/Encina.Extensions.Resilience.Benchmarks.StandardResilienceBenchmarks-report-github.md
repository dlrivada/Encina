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
| NoResilience_Baseline                         |  71.42 ms |    NA |  1.00 |    1 |   7.02 KB |        1.00 |
| StandardResilience_Success                    | 181.10 ms |    NA |  2.54 |    2 |   5.42 KB |        0.77 |
| StandardResilience_MultipleSequentialRequests | 181.89 ms |    NA |  2.55 |    3 |   14.9 KB |        2.12 |
| StandardResilience_ConcurrentRequests         | 186.17 ms |    NA |  2.61 |    4 |  16.36 KB |        2.33 |
| StandardResilience_WithRetry                  | 205.37 ms |    NA |  2.88 |    5 |  18.23 KB |        2.60 |
