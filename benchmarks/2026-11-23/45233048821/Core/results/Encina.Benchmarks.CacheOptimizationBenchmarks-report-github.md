```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Median        | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.9089 ns |   0.9911 ns |  0.0543 ns |    41.8825 ns |   0.980 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.5816 ns |   3.1836 ns |  0.1745 ns |    13.5702 ns |   0.318 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2897 ns |   3.3047 ns |  0.1811 ns |     0.3942 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,600.1106 ns | 821.1038 ns | 45.0075 ns | 4,585.1333 ns | 107.600 |    1.19 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.7539 ns |   6.3902 ns |  0.3503 ns |    42.7266 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,773.2525 ns | 453.4216 ns | 24.8536 ns | 3,774.9462 ns |  88.259 |    0.80 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,727.8105 ns | 198.9351 ns | 10.9043 ns | 4,727.8647 ns | 110.587 |    0.81 | 0.1678 |    2832 B |          NA |
