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
| Cache_TryGetValue_ThenGetOrAdd |    41.5726 ns |   1.0649 ns |  0.0584 ns |  0.890 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4990 ns |   0.0638 ns |  0.0035 ns |  0.268 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3484 ns |   0.0099 ns |  0.0005 ns |  0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,999.1414 ns | 210.6459 ns | 11.5462 ns | 85.610 |    0.36 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.7138 ns |   3.2712 ns |  0.1793 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,264.9355 ns | 211.1657 ns | 11.5747 ns | 69.893 |    0.32 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,122.6763 ns | 209.5534 ns | 11.4863 ns | 88.255 |    0.36 | 0.1526 |    2600 B |          NA |
