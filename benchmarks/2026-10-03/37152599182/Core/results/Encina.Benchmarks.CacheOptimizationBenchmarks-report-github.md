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
| Cache_TryGetValue_ThenGetOrAdd |    46.6228 ns |   0.0272 ns |  0.0015 ns |  0.989 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.2380 ns |   3.2589 ns |  0.1786 ns |  0.260 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0000 ns |   0.0000 ns |  0.0000 ns |  0.000 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,885.4203 ns | 113.5866 ns |  6.2261 ns | 82.432 |    0.12 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    47.1350 ns |   0.5898 ns |  0.0323 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,283.6015 ns | 115.0825 ns |  6.3081 ns | 69.664 |    0.12 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,084.4413 ns | 501.4076 ns | 27.4838 ns | 86.654 |    0.51 | 0.1526 |    2600 B |          NA |
