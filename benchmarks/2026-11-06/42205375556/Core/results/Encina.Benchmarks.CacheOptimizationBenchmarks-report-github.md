```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    24.4575 ns |     6.1344 ns |   0.3362 ns |   0.978 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     7.4750 ns |     2.0513 ns |   0.1124 ns |   0.299 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2327 ns |     0.1183 ns |   0.0065 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,767.1586 ns |   180.9132 ns |   9.9165 ns | 110.674 |    2.21 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    25.0116 ns |    10.4579 ns |   0.5732 ns |   1.000 |    0.03 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,296.9585 ns |   184.5647 ns |  10.1166 ns |  91.868 |    1.85 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,932.7136 ns | 3,030.6165 ns | 166.1183 ns | 117.295 |    6.20 | 0.1678 |    2832 B |          NA |
