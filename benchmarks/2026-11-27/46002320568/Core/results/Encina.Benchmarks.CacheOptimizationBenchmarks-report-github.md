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
| Cache_TryGetValue_ThenGetOrAdd |    41.1831 ns |   0.3026 ns |  0.0166 ns |   0.991 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.4161 ns |   0.1161 ns |  0.0064 ns |   0.323 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3484 ns |   0.2064 ns |  0.0113 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,613.9678 ns |  46.2086 ns |  2.5328 ns | 111.048 |    0.19 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.5492 ns |   1.4333 ns |  0.0786 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,685.5092 ns | 106.8462 ns |  5.8566 ns |  88.702 |    0.19 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,747.7175 ns | 524.3259 ns | 28.7401 ns | 114.268 |    0.63 | 0.1678 |    2832 B |          NA |
