```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.4971 ns |   7.2696 ns |  0.3985 ns |    33.5635 ns |  0.868 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.4361 ns |   1.1952 ns |  0.0655 ns |    10.4005 ns |  0.270 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2141 ns |   2.3945 ns |  0.1312 ns |     0.2895 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,249.0146 ns | 117.6812 ns |  6.4505 ns | 3,252.0179 ns | 84.204 |    0.45 | 0.1564 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    38.5861 ns |   4.0805 ns |  0.2237 ns |    38.4619 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,655.0990 ns | 588.7455 ns | 32.2711 ns | 2,640.3865 ns | 68.811 |    0.80 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 3,395.1532 ns | 434.6632 ns | 23.8254 ns | 3,384.3441 ns | 87.991 |    0.69 | 0.1526 |    2600 B |          NA |
