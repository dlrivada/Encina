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
| Cache_TryGetValue_ThenGetOrAdd |    41.0571 ns |  10.3588 ns | 0.5678 ns |  0.934 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1525 ns |   0.2406 ns | 0.0132 ns |  0.276 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2729 ns |   0.0564 ns | 0.0031 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,927.8445 ns | 129.6669 ns | 7.1075 ns | 89.329 |    1.40 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    43.9801 ns |  14.5648 ns | 0.7983 ns |  1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,256.9280 ns | 112.0092 ns | 6.1396 ns | 74.071 |    1.16 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,129.2183 ns | 104.0328 ns | 5.7024 ns | 93.909 |    1.47 | 0.1526 |    2600 B |          NA |
