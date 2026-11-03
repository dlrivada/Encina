```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    24.4862 ns |  14.8729 ns |  0.8152 ns |   1.03 |    0.03 |      - |         - |          NA |
| TypeCheck_Cached               |     7.4899 ns |   2.9393 ns |  0.1611 ns |   0.31 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2564 ns |   0.0723 ns |  0.0040 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,682.3360 ns | 440.3009 ns | 24.1344 ns | 112.43 |    1.58 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    23.8606 ns |   5.8566 ns |  0.3210 ns |   1.00 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,180.2658 ns | 101.9220 ns |  5.5867 ns |  91.39 |    1.09 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,755.6423 ns | 286.3824 ns | 15.6976 ns | 115.50 |    1.47 | 0.1678 |    2832 B |          NA |
