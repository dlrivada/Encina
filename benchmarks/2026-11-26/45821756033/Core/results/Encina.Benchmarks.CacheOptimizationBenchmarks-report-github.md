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
| Cache_TryGetValue_ThenGetOrAdd |    30.6566 ns |   0.8720 ns |  0.0478 ns |  0.950 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.3377 ns |   0.0309 ns |  0.0017 ns |  0.320 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2099 ns |   0.0217 ns |  0.0012 ns |  0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,614.6263 ns | 107.9191 ns |  5.9154 ns | 81.047 |    0.20 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.2605 ns |   0.9764 ns |  0.0535 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,094.3042 ns | 264.3828 ns | 14.4917 ns | 64.919 |    0.40 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,701.7837 ns |  73.2857 ns |  4.0170 ns | 83.749 |    0.16 | 0.0305 |    2832 B |          NA |
