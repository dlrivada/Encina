```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.1504 ns |   0.5182 ns |  0.0284 ns |   0.957 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.4308 ns |   0.2318 ns |  0.0127 ns |   0.312 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3349 ns |   0.0671 ns |  0.0037 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,551.6939 ns | 360.3209 ns | 19.7504 ns | 105.850 |    1.19 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    43.0055 ns |   9.4932 ns |  0.5204 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,513.2748 ns | 295.8735 ns | 16.2178 ns |  81.702 |    0.92 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,670.3699 ns | 639.5874 ns | 35.0579 ns | 108.610 |    1.35 | 0.1678 |    2832 B |          NA |
