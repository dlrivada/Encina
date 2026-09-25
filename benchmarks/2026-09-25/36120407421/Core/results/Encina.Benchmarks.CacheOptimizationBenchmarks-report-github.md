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
| Cache_TryGetValue_ThenGetOrAdd |    43.8846 ns |   1.1423 ns |  0.0626 ns |  0.941 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5399 ns |   0.6505 ns |  0.0357 ns |  0.269 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3585 ns |   0.2279 ns |  0.0125 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,078.0261 ns | 146.1552 ns |  8.0113 ns | 87.457 |    0.15 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.6288 ns |   0.2712 ns |  0.0149 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,220.7294 ns |  38.2220 ns |  2.0951 ns | 69.072 |    0.04 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,103.1389 ns | 437.5910 ns | 23.9858 ns | 87.996 |    0.45 | 0.1526 |    2600 B |          NA |
