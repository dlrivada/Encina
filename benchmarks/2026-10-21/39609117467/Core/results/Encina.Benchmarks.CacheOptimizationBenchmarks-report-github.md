```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    23.0366 ns |    12.0303 ns |  0.6594 ns |   0.96 |    0.03 |      - |         - |          NA |
| TypeCheck_Cached               |     7.1105 ns |     1.5266 ns |  0.0837 ns |   0.30 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3273 ns |     1.9127 ns |  0.1048 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,586.0923 ns |   508.2548 ns | 27.8592 ns | 107.48 |    1.54 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    24.0635 ns |     5.5261 ns |  0.3029 ns |   1.00 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,142.5774 ns | 1,529.9419 ns | 83.8613 ns |  89.05 |    3.17 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,740.8185 ns | 1,435.1051 ns | 78.6630 ns | 113.91 |    3.09 | 0.1678 |    2832 B |          NA |
