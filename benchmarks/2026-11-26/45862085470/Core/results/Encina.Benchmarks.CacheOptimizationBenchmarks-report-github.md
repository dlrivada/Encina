```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    26.7656 ns |   4.7200 ns |  0.2587 ns |   0.972 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     8.1676 ns |   3.9251 ns |  0.2151 ns |   0.297 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2228 ns |   0.2467 ns |  0.0135 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,919.1212 ns | 195.1702 ns | 10.6979 ns | 105.990 |    1.35 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    27.5451 ns |   7.1563 ns |  0.3923 ns |   1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,310.8347 ns | 353.8901 ns | 19.3979 ns |  83.904 |    1.20 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,945.6536 ns | 362.1849 ns | 19.8526 ns | 106.954 |    1.46 | 0.1678 |    2832 B |          NA |
