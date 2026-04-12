```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.5529 ns |  0.0499 ns |  0.0684 ns |  0.900 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1520 ns |  0.0135 ns |  0.0184 ns |  0.251 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2728 ns |  0.0026 ns |  0.0034 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,522.5202 ns | 30.2542 ns | 43.3897 ns | 72.756 |    0.93 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    48.4160 ns |  0.1376 ns |  0.2059 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,959.8784 ns |  6.6566 ns |  9.7572 ns | 40.481 |    0.26 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,653.5113 ns | 14.4275 ns | 21.5944 ns | 75.462 |    0.54 | 0.1335 |    2248 B |          NA |
