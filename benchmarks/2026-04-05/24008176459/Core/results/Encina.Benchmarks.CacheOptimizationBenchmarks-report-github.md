```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                         | Mean       | Error | Ratio | Allocated | Alloc Ratio |
|------------------------------- |-----------:|------:|------:|----------:|------------:|
| Cache_GetOrAdd_Direct          |   749.3 μs |    NA |  1.00 |         - |          NA |
| Cache_TryGetValue_ThenGetOrAdd |   649.0 μs |    NA |  0.87 |         - |          NA |
| Send_Command_CacheHit          | 5,404.5 μs |    NA |  7.21 |    2488 B |          NA |
| Send_Query_CacheHit            | 5,543.8 μs |    NA |  7.40 |    2464 B |          NA |
| Publish_Notification_CacheHit  | 3,755.5 μs |    NA |  5.01 |    1616 B |          NA |
| TypeCheck_Cached               |   925.8 μs |    NA |  1.24 |         - |          NA |
| TypeCheck_Direct               |   256.4 μs |    NA |  0.34 |         - |          NA |
