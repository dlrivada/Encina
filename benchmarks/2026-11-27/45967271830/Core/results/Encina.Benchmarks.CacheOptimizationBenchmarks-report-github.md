```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    34.9038 ns |     5.3162 ns |  0.2914 ns |   1.046 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.7455 ns |     1.1948 ns |  0.0655 ns |   0.322 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0398 ns |     0.3344 ns |  0.0183 ns |   0.001 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,679.2596 ns | 1,551.5115 ns | 85.0436 ns | 110.239 |    2.46 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.3782 ns |     6.8576 ns |  0.3759 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,937.1541 ns |   239.4311 ns | 13.1240 ns |  88.004 |    0.93 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,696.1022 ns |   394.5932 ns | 21.6290 ns | 110.743 |    1.22 | 0.1678 |    2832 B |          NA |
