```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.7760 ns |   2.9437 ns | 0.1614 ns |  0.939 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5373 ns |   0.7374 ns | 0.0404 ns |  0.269 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3512 ns |   0.0702 ns | 0.0038 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,070.0066 ns | 107.4834 ns | 5.8915 ns | 87.287 |    0.11 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.6281 ns |   0.0390 ns | 0.0021 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,223.6199 ns | 125.6994 ns | 6.8900 ns | 69.135 |    0.13 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,219.4815 ns |  75.8867 ns | 4.1596 ns | 90.492 |    0.08 | 0.1526 |    2600 B |          NA |
