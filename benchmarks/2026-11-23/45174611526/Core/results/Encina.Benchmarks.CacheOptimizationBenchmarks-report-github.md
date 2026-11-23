```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.6752 ns |   4.9775 ns |  0.2728 ns |   1.02 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.6386 ns |   1.3889 ns |  0.0761 ns |   0.32 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3994 ns |   3.0082 ns |  0.1649 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,670.2310 ns | 314.1475 ns | 17.2195 ns | 111.29 |    0.46 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.9780 ns |   0.4820 ns |  0.0264 ns |   1.00 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,901.2961 ns | 351.3419 ns | 19.2582 ns |  87.98 |    0.51 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,838.1199 ns | 295.0767 ns | 16.1741 ns | 116.38 |    0.43 | 0.1678 |    2832 B |          NA |
