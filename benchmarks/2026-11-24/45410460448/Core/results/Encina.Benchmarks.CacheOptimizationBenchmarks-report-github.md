```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    30.8963 ns |   0.5942 ns | 0.0326 ns |  0.950 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.4289 ns |   0.4634 ns | 0.0254 ns |  0.321 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2075 ns |   0.0771 ns | 0.0042 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,624.8040 ns |  31.0187 ns | 1.7002 ns | 80.704 |    0.29 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.5240 ns |   2.4621 ns | 0.1350 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,110.8490 ns | 109.1452 ns | 5.9826 ns | 64.902 |    0.28 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,697.8469 ns |  33.1112 ns | 1.8149 ns | 82.950 |    0.30 | 0.0305 |    2832 B |          NA |
