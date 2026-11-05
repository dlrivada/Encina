```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Median        | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    45.4556 ns |     1.3524 ns |  0.0741 ns |    45.4863 ns |   1.056 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.7562 ns |     1.8524 ns |  0.1015 ns |    12.7667 ns |   0.296 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1255 ns |     3.6314 ns |  0.1991 ns |     0.0216 ns |   0.003 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,646.6444 ns | 1,273.2648 ns | 69.7919 ns | 4,622.7632 ns | 107.992 |    1.44 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    43.0282 ns |     2.6695 ns |  0.1463 ns |    43.0868 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,787.1119 ns |   277.3450 ns | 15.2022 ns | 3,787.0972 ns |  88.015 |    0.40 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,816.7665 ns |   804.0203 ns | 44.0711 ns | 4,792.8849 ns | 111.945 |    0.95 | 0.1678 |    2832 B |          NA |
