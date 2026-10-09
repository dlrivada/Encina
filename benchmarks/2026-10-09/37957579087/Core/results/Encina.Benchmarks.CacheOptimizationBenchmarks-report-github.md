```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    24.2892 ns |    11.2398 ns |  0.6161 ns |   0.996 |    0.03 |      - |         - |          NA |
| TypeCheck_Cached               |     7.6544 ns |     6.6880 ns |  0.3666 ns |   0.314 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2212 ns |     0.4351 ns |  0.0239 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,400.7693 ns | 1,566.6086 ns | 85.8711 ns |  98.472 |    3.31 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    24.3838 ns |     6.6919 ns |  0.3668 ns |   1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,044.5005 ns |   588.7353 ns | 32.2706 ns |  83.859 |    1.59 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,585.8257 ns |   950.4895 ns | 52.0995 ns | 106.063 |    2.31 | 0.1755 |    2936 B |          NA |
