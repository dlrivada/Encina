```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.6269 ns |   1.6617 ns |  0.0911 ns |   0.919 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4574 ns |   0.1209 ns |  0.0066 ns |   0.275 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2730 ns |   0.0146 ns |  0.0008 ns |   0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,426.5117 ns | 182.5529 ns | 10.0063 ns |  97.697 |    0.29 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    45.3088 ns |   2.1939 ns |  0.1203 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,598.3935 ns | 623.4014 ns | 34.1707 ns |  79.420 |    0.68 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,611.3820 ns | 199.0934 ns | 10.9130 ns | 101.777 |    0.31 | 0.1678 |    2832 B |          NA |
