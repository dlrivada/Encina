```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    23.6291 ns |     3.0463 ns |  0.1670 ns |   0.971 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     7.1210 ns |     7.8818 ns |  0.4320 ns |   0.293 |    0.02 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1824 ns |     0.0256 ns |  0.0014 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,617.2564 ns | 1,714.8274 ns | 93.9955 ns | 107.581 |    4.22 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    24.3406 ns |    12.2562 ns |  0.6718 ns |   1.001 |    0.03 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,026.7674 ns |   566.4463 ns | 31.0488 ns |  83.309 |    2.27 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,570.4201 ns |   656.2708 ns | 35.9724 ns | 105.656 |    2.83 | 0.1678 |    2832 B |          NA |
