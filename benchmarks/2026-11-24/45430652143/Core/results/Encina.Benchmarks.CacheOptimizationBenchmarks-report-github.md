```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.12GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    30.7300 ns |   0.7989 ns |  0.0438 ns |  0.956 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.3995 ns |   0.2684 ns |  0.0147 ns |  0.324 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2096 ns |   0.1247 ns |  0.0068 ns |  0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,596.3043 ns | 156.7875 ns |  8.5941 ns | 80.805 |    0.27 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.1306 ns |   1.1657 ns |  0.0639 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,101.0801 ns |  46.3527 ns |  2.5407 ns | 65.392 |    0.13 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,696.0667 ns | 231.9695 ns | 12.7150 ns | 83.910 |    0.37 | 0.0305 |    2832 B |          NA |
