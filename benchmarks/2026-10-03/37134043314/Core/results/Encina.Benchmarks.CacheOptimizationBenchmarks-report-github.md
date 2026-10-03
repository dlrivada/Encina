```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    46.6307 ns |   1.2633 ns | 0.0692 ns |  1.013 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1860 ns |   1.0330 ns | 0.0566 ns |  0.265 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2704 ns |   0.0506 ns | 0.0028 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,985.2556 ns | 147.8159 ns | 8.1023 ns | 86.563 |    1.54 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.0516 ns |  17.4046 ns | 0.9540 ns |  1.000 |    0.03 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,192.4555 ns |  70.7047 ns | 3.8756 ns | 69.343 |    1.23 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,061.0146 ns | 139.5210 ns | 7.6476 ns | 88.209 |    1.57 | 0.1526 |    2600 B |          NA |
