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
| Cache_TryGetValue_ThenGetOrAdd |    41.4682 ns |   1.7765 ns |  0.0974 ns |   0.982 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.6358 ns |   6.2573 ns |  0.3430 ns |   0.323 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3684 ns |   0.2198 ns |  0.0120 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,575.1325 ns | 144.9711 ns |  7.9464 ns | 108.316 |    0.82 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.2410 ns |   6.6722 ns |  0.3657 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,717.7354 ns | 195.1889 ns | 10.6990 ns |  88.017 |    0.69 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,873.8639 ns | 242.2810 ns | 13.2802 ns | 115.388 |    0.90 | 0.1678 |    2832 B |          NA |
