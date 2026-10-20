```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.3558 ns |   0.8469 ns |  0.0464 ns |   0.974 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.4013 ns |   4.3359 ns |  0.2377 ns |   0.304 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2914 ns |   0.0138 ns |  0.0008 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,522.4979 ns | 356.0051 ns | 19.5138 ns | 102.850 |    0.50 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    34.2488 ns |   0.3615 ns |  0.0198 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,805.7149 ns | 969.0471 ns | 53.1167 ns |  81.921 |    1.34 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,637.8814 ns | 373.3351 ns | 20.4638 ns | 106.219 |    0.52 | 0.1678 |    2832 B |          NA |
