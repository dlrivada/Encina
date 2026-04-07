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
| Cache_TryGetValue_ThenGetOrAdd |   493.9 μs |    NA |  0.68 |         - |          NA |
| TypeCheck_Cached               |   900.7 μs |    NA |  1.24 |         - |          NA |
| TypeCheck_Direct               |   245.2 μs |    NA |  0.34 |         - |          NA |
| Send_Command_CacheHit          | 5,290.9 μs |    NA |  7.30 |    2488 B |          NA |
| Cache_GetOrAdd_Direct          |   725.3 μs |    NA |  1.00 |         - |          NA |
| Publish_Notification_CacheHit  | 3,782.9 μs |    NA |  5.22 |    1616 B |          NA |
| Send_Query_CacheHit            | 5,058.4 μs |    NA |  6.97 |    2464 B |          NA |
