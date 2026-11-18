```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Median        | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    23.4214 ns |     6.1818 ns |   0.3388 ns |    23.5045 ns |   0.941 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     7.7381 ns |     3.2901 ns |   0.1803 ns |     7.7028 ns |   0.311 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1542 ns |     1.7269 ns |   0.0947 ns |     0.1173 ns |   0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,723.3410 ns | 2,512.3253 ns | 137.7090 ns | 2,679.4653 ns | 109.394 |    4.95 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    24.8975 ns |     5.9216 ns |   0.3246 ns |    24.8884 ns |   1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,046.0484 ns | 1,115.7452 ns |  61.1578 ns | 2,046.0535 ns |  82.188 |    2.32 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,812.8587 ns | 2,916.8926 ns | 159.8847 ns | 2,852.8733 ns | 112.990 |    5.71 | 0.1678 |    2832 B |          NA |
