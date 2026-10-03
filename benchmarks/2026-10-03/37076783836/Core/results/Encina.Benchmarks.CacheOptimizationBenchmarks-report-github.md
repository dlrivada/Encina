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
| Cache_TryGetValue_ThenGetOrAdd |    46.4467 ns |   1.9293 ns |  0.1057 ns |  1.013 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1507 ns |   0.2121 ns |  0.0116 ns |  0.265 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2711 ns |   0.0230 ns |  0.0013 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,975.5967 ns |  96.1604 ns |  5.2709 ns | 86.703 |    0.10 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    45.8531 ns |   0.3196 ns |  0.0175 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,279.6314 ns | 253.9715 ns | 13.9210 ns | 71.525 |    0.26 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,156.1758 ns | 424.4678 ns | 23.2665 ns | 90.641 |    0.44 | 0.1526 |    2600 B |          NA |
