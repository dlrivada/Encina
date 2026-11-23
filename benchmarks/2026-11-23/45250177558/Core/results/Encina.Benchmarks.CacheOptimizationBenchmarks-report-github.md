```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.1155 ns |   4.4994 ns |  0.2466 ns |   0.974 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.4630 ns |   1.1620 ns |  0.0637 ns |   0.319 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3700 ns |   0.0251 ns |  0.0014 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,625.7628 ns | 293.8624 ns | 16.1076 ns | 109.626 |    0.58 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.1966 ns |   3.8773 ns |  0.2125 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,568.4325 ns | 159.8750 ns |  8.7633 ns |  84.568 |    0.41 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,841.2160 ns |  49.4569 ns |  2.7109 ns | 114.732 |    0.50 | 0.1678 |    2832 B |          NA |
