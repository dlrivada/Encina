```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.5314 ns |   4.5296 ns |  0.2483 ns |  0.950 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1421 ns |   0.0553 ns |  0.0030 ns |  0.278 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2760 ns |   0.1137 ns |  0.0062 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,033.7160 ns | 129.8722 ns |  7.1187 ns | 92.273 |    0.83 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    43.7182 ns |   8.1704 ns |  0.4478 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,250.0371 ns | 114.5653 ns |  6.2797 ns | 74.346 |    0.67 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,305.8742 ns | 507.5829 ns | 27.8223 ns | 98.498 |    1.03 | 0.1526 |    2600 B |          NA |
