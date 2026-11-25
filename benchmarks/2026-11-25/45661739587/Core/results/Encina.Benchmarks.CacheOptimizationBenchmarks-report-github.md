```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.0615 ns |   0.1544 ns |  0.0085 ns |   0.974 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.4565 ns |   0.3880 ns |  0.0213 ns |   0.319 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3641 ns |   0.4484 ns |  0.0246 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,546.4845 ns | 482.9630 ns | 26.4728 ns | 107.884 |    0.77 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.1431 ns |   4.4594 ns |  0.2444 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,600.6875 ns |  27.0754 ns |  1.4841 ns |  85.441 |    0.43 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,894.8253 ns | 275.8157 ns | 15.1184 ns | 116.150 |    0.66 | 0.1678 |    2832 B |          NA |
