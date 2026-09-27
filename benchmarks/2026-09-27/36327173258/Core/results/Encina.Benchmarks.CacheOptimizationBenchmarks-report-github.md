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
| Cache_TryGetValue_ThenGetOrAdd |    43.6374 ns |   3.1762 ns |  0.1741 ns |  0.936 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5128 ns |   0.1038 ns |  0.0057 ns |  0.268 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3527 ns |   0.1882 ns |  0.0103 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,993.7513 ns |  30.3175 ns |  1.6618 ns | 85.687 |    0.04 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.6086 ns |   0.2547 ns |  0.0140 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,257.8196 ns | 240.0786 ns | 13.1595 ns | 69.897 |    0.25 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,111.2850 ns | 360.8046 ns | 19.7769 ns | 88.209 |    0.37 | 0.1526 |    2600 B |          NA |
