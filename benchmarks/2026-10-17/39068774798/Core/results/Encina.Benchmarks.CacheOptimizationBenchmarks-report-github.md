```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    36.3421 ns |   2.4176 ns | 0.1325 ns |   0.92 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4565 ns |   0.0889 ns | 0.0049 ns |   0.31 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.5116 ns |   0.5971 ns | 0.0327 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,050.4095 ns |  58.8919 ns | 3.2281 ns | 102.41 |    0.07 | 0.1068 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    39.5519 ns |   0.0901 ns | 0.0049 ns |   1.00 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,199.4823 ns | 156.1990 ns | 8.5618 ns |  80.89 |    0.19 | 0.0877 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,239.6864 ns |   8.3801 ns | 0.4593 ns | 107.19 |    0.02 | 0.1068 |    2832 B |          NA |
