```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Median        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|--------------:|------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    44.0380 ns |   2.6732 ns | 0.1465 ns |    44.0995 ns |  1.01 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.0180 ns |   1.8863 ns | 0.1034 ns |    12.9632 ns |  0.30 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.4479 ns |   3.7510 ns | 0.2056 ns |     0.3304 ns |  0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,187.5600 ns | 106.6215 ns | 5.8443 ns | 4,184.7046 ns | 96.40 |    0.23 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    43.4405 ns |   1.9164 ns | 0.1050 ns |    43.4511 ns |  1.00 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,406.6702 ns | 106.5349 ns | 5.8395 ns | 3,405.9689 ns | 78.42 |    0.20 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,313.5754 ns | 134.4311 ns | 7.3686 ns | 4,314.1305 ns | 99.30 |    0.25 | 0.1526 |    2600 B |          NA |
