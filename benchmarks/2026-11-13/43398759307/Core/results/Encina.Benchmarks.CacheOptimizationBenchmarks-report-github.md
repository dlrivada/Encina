```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.9157 ns |   9.9790 ns |  0.5470 ns |   1.003 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.4884 ns |   1.1051 ns |  0.0606 ns |   0.323 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3478 ns |   0.0455 ns |  0.0025 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,468.3288 ns | 174.5809 ns |  9.5694 ns | 106.950 |    0.21 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.7796 ns |   0.5253 ns |  0.0288 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,617.4139 ns | 160.9152 ns |  8.8203 ns |  86.583 |    0.19 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,635.5026 ns | 237.0652 ns | 12.9943 ns | 110.951 |    0.28 | 0.1678 |    2832 B |          NA |
