```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.2807 ns |   2.3417 ns |  0.1284 ns |   0.987 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.1392 ns |   0.1787 ns |  0.0098 ns |   0.314 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3551 ns |   0.1585 ns |  0.0087 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,622.4076 ns | 260.1600 ns | 14.2602 ns | 110.471 |    0.31 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.8426 ns |   0.8752 ns |  0.0480 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,648.5437 ns | 100.4613 ns |  5.5066 ns |  87.197 |    0.14 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,670.7057 ns |  30.9595 ns |  1.6970 ns | 111.626 |    0.12 | 0.1678 |    2832 B |          NA |
