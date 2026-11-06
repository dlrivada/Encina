```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.6380 ns |  14.5719 ns |  0.7987 ns |   1.006 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4825 ns |   0.7850 ns |  0.0430 ns |   0.302 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2723 ns |   0.0328 ns |  0.0018 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,438.6051 ns | 385.7491 ns | 21.1442 ns | 107.236 |    0.44 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.3909 ns |   0.2248 ns |  0.0123 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,627.9370 ns | 294.3154 ns | 16.1324 ns |  87.651 |    0.34 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,746.5091 ns | 241.9905 ns | 13.2643 ns | 114.675 |    0.28 | 0.1678 |    2832 B |          NA |
