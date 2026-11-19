```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    32.9212 ns |   2.6941 ns |  0.1477 ns |   1.000 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.6237 ns |   0.6932 ns |  0.0380 ns |   0.323 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3098 ns |   0.2730 ns |  0.0150 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,595.3987 ns | 290.2489 ns | 15.9095 ns | 109.162 |    0.43 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.9364 ns |   0.6523 ns |  0.0358 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,893.5092 ns | 212.0713 ns | 11.6243 ns |  87.851 |    0.32 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,769.1045 ns |  77.2210 ns |  4.2327 ns | 114.436 |    0.15 | 0.1678 |    2832 B |          NA |
