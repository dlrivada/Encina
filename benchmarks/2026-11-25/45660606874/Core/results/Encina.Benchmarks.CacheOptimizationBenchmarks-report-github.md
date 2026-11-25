```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    31.0309 ns |   1.1313 ns |  0.0620 ns |  0.959 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.3746 ns |   0.3672 ns |  0.0201 ns |  0.321 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2010 ns |   0.1087 ns |  0.0060 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,621.6012 ns | 213.1245 ns | 11.6821 ns | 81.011 |    0.36 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.3614 ns |   1.4194 ns |  0.0778 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,194.1028 ns | 130.3518 ns |  7.1450 ns | 67.800 |    0.24 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,744.9202 ns |  71.0375 ns |  3.8938 ns | 84.821 |    0.21 | 0.0305 |    2832 B |          NA |
