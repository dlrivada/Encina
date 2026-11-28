```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.36GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    30.8544 ns |   0.6523 ns | 0.0358 ns |  0.949 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.4033 ns |   0.3375 ns | 0.0185 ns |  0.320 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2036 ns |   0.0201 ns | 0.0011 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,585.0000 ns |  71.8418 ns | 3.9379 ns | 79.538 |    0.15 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.5001 ns |   0.8667 ns | 0.0475 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,176.7308 ns |  41.7310 ns | 2.2874 ns | 66.976 |    0.10 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,740.1881 ns | 158.8330 ns | 8.7062 ns | 84.313 |    0.26 | 0.0305 |    2832 B |          NA |
