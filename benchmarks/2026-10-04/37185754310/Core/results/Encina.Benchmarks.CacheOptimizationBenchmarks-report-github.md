```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    47.0817 ns |  13.6519 ns |  0.7483 ns |  1.033 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4223 ns |   4.1332 ns |  0.2266 ns |  0.273 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2779 ns |   0.1743 ns |  0.0096 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,028.3521 ns | 314.0749 ns | 17.2155 ns | 88.380 |    0.33 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    45.5800 ns |   0.2641 ns |  0.0145 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,226.5697 ns | 139.5275 ns |  7.6480 ns | 70.789 |    0.15 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,188.9356 ns | 107.4828 ns |  5.8915 ns | 91.903 |    0.11 | 0.1526 |    2600 B |          NA |
