```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.5262 ns |   8.2247 ns |  0.4508 ns |   0.991 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.5213 ns |   1.9630 ns |  0.1076 ns |   0.323 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3339 ns |   0.0159 ns |  0.0009 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,393.0443 ns | 152.0411 ns |  8.3339 ns | 104.847 |    0.34 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.8997 ns |   2.4802 ns |  0.1359 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,576.8184 ns | 212.9644 ns | 11.6733 ns |  85.367 |    0.34 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,714.4372 ns | 332.2933 ns | 18.2141 ns | 112.518 |    0.49 | 0.1678 |    2832 B |          NA |
