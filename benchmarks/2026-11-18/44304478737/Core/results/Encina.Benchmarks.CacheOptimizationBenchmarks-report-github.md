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
| Cache_TryGetValue_ThenGetOrAdd |    41.2927 ns |   0.4960 ns |  0.0272 ns |   0.979 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.4345 ns |   0.1844 ns |  0.0101 ns |   0.319 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3695 ns |   0.3383 ns |  0.0185 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,644.6676 ns | 250.3571 ns | 13.7229 ns | 110.146 |    0.30 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.1683 ns |   0.7832 ns |  0.0429 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,541.6877 ns | 185.9403 ns | 10.1920 ns |  83.989 |    0.22 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,673.3495 ns | 332.1866 ns | 18.2083 ns | 110.826 |    0.39 | 0.1678 |    2832 B |          NA |
