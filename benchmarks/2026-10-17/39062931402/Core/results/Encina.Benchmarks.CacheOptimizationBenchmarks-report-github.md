```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.4609 ns |   2.7281 ns | 0.1495 ns |   1.000 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.1329 ns |   0.1085 ns | 0.0059 ns |   0.317 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3491 ns |   0.0088 ns | 0.0005 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,463.6589 ns | 178.4102 ns | 9.7793 ns | 107.619 |    0.40 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.4768 ns |   2.7547 ns | 0.1510 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,546.6186 ns |  26.2116 ns | 1.4367 ns |  85.509 |    0.27 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,706.2233 ns | 172.3815 ns | 9.4488 ns | 113.468 |    0.41 | 0.1678 |    2832 B |          NA |
