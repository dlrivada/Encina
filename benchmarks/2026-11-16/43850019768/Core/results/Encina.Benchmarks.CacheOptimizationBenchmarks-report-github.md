```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.8051 ns |  12.2689 ns |  0.6725 ns |   0.998 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.5552 ns |   2.2323 ns |  0.1224 ns |   0.324 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3519 ns |   0.0547 ns |  0.0030 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,470.0589 ns | 766.8961 ns | 42.0362 ns | 106.745 |    0.87 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.8760 ns |   0.6307 ns |  0.0346 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,526.8465 ns |  82.9210 ns |  4.5452 ns |  84.221 |    0.11 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,645.6369 ns | 269.0617 ns | 14.7482 ns | 110.938 |    0.32 | 0.1678 |    2832 B |          NA |
