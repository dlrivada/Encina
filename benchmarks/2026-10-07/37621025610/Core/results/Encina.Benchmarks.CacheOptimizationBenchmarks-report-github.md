```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.0686 ns |   2.0371 ns |  0.1117 ns |   1.009 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.2492 ns |   0.8669 ns |  0.0475 ns |   0.294 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3533 ns |   0.1201 ns |  0.0066 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,353.5705 ns | 277.0279 ns | 15.1848 ns | 104.399 |    0.35 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    41.7012 ns |   1.2004 ns |  0.0658 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,605.9727 ns | 179.7296 ns |  9.8516 ns |  86.472 |    0.24 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,414.8356 ns | 282.7219 ns | 15.4969 ns | 105.868 |    0.35 | 0.1755 |    2936 B |          NA |
