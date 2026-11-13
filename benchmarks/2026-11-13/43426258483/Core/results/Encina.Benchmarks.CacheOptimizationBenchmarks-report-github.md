```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.91GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.3413 ns |   0.5965 ns |  0.0327 ns |   0.982 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.4525 ns |   0.2898 ns |  0.0159 ns |   0.319 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3481 ns |   0.3103 ns |  0.0170 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,430.4992 ns | 535.2594 ns | 29.3394 ns | 105.205 |    1.17 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.1165 ns |   8.4396 ns |  0.4626 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,612.4366 ns | 176.5561 ns |  9.6776 ns |  85.779 |    0.84 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,691.1186 ns | 521.4650 ns | 28.5833 ns | 111.393 |    1.22 | 0.1678 |    2832 B |          NA |
