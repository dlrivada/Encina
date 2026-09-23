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
| Cache_TryGetValue_ThenGetOrAdd |    41.2988 ns |   4.1222 ns |  0.2260 ns |  0.938 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1918 ns |   0.2922 ns |  0.0160 ns |  0.277 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2717 ns |   0.0220 ns |  0.0012 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,032.1103 ns | 269.5426 ns | 14.7745 ns | 91.620 |    0.37 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    44.0094 ns |   2.3664 ns |  0.1297 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,273.6527 ns |  38.5250 ns |  2.1117 ns | 74.386 |    0.19 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,285.5457 ns | 254.4959 ns | 13.9498 ns | 97.379 |    0.37 | 0.1526 |    2600 B |          NA |
