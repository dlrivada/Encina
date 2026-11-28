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
| Cache_TryGetValue_ThenGetOrAdd |    33.1030 ns |   1.6646 ns |  0.0912 ns |   1.012 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |     9.8558 ns |   0.1018 ns |  0.0056 ns |   0.301 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3020 ns |   0.3420 ns |  0.0187 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,606.4274 ns | 254.4453 ns | 13.9470 ns | 110.256 |    0.37 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.7095 ns |   0.2852 ns |  0.0156 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,921.8493 ns | 115.1486 ns |  6.3117 ns |  89.327 |    0.17 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,705.5628 ns | 286.9851 ns | 15.7306 ns | 113.287 |    0.42 | 0.1678 |    2832 B |          NA |
