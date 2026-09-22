```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.8661 ns |  12.5192 ns |  0.6862 ns |    34.1941 ns |  0.950 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    10.5894 ns |   1.6295 ns |  0.0893 ns |    10.6096 ns |  0.297 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1487 ns |   2.5791 ns |  0.1414 ns |     0.0974 ns |  0.004 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,949.8367 ns | 307.1900 ns | 16.8381 ns | 2,954.3744 ns | 82.789 |    0.47 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    35.6309 ns |   2.1425 ns |  0.1174 ns |    35.5701 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,479.4565 ns |  41.9983 ns |  2.3021 ns | 1,479.6241 ns | 41.522 |    0.13 | 0.0820 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,001.3593 ns | 217.9174 ns | 11.9448 ns | 2,998.7458 ns | 84.235 |    0.38 | 0.1335 |    2248 B |          NA |
