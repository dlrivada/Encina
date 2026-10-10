```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.1105 ns |   0.2804 ns |  0.0154 ns |  0.850 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5072 ns |   0.1682 ns |  0.0092 ns |  0.253 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3583 ns |   0.1694 ns |  0.0093 ns |  0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,084.7564 ns | 331.8876 ns | 18.1919 ns | 82.473 |    0.32 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    49.5285 ns |   0.6917 ns |  0.0379 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,434.0982 ns | 233.2676 ns | 12.7862 ns | 69.336 |    0.23 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,228.4701 ns | 154.5397 ns |  8.4708 ns | 85.375 |    0.16 | 0.1755 |    2936 B |          NA |
