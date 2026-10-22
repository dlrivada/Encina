```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    22.8741 ns |   3.2747 ns |  0.1795 ns |   0.96 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     7.1500 ns |   5.6771 ns |  0.3112 ns |   0.30 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2898 ns |   1.0840 ns |  0.0594 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,465.5446 ns | 218.4019 ns | 11.9713 ns | 103.80 |    0.74 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    23.7527 ns |   2.8793 ns |  0.1578 ns |   1.00 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,955.3546 ns | 407.2908 ns | 22.3250 ns |  82.32 |    0.94 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,691.9507 ns |  27.8220 ns |  1.5250 ns | 113.34 |    0.65 | 0.1678 |    2832 B |          NA |
