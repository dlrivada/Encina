```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.4598 ns |  11.8087 ns |  0.6473 ns |    33.2102 ns |  0.968 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     9.9532 ns |   2.5070 ns |  0.1374 ns |     9.9310 ns |  0.288 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3278 ns |   2.9113 ns |  0.1596 ns |     0.2360 ns |  0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,242.5433 ns |  30.5976 ns |  1.6772 ns | 3,242.9218 ns | 93.844 |    0.28 | 0.1564 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    34.5526 ns |   2.1190 ns |  0.1162 ns |    34.5676 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,678.9175 ns | 211.8771 ns | 11.6137 ns | 2,683.9645 ns | 77.532 |    0.37 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 3,356.5646 ns | 160.2662 ns |  8.7847 ns | 3,360.5667 ns | 97.144 |    0.36 | 0.1526 |    2600 B |          NA |
