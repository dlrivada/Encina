```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    23.0194 ns |  0.3159 ns |  0.4630 ns |  0.98 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     7.0578 ns |  0.1788 ns |  0.2621 ns |  0.30 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2877 ns |  0.0478 ns |  0.0716 ns |  0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,222.4596 ns | 19.2202 ns | 26.3088 ns | 94.21 |    1.88 | 0.1564 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    23.5962 ns |  0.2751 ns |  0.3945 ns |  1.00 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,849.9345 ns | 29.3369 ns | 41.1262 ns | 78.42 |    2.13 | 0.1125 |    1896 B |          NA |
| Send_Query_CacheHit            | 2,279.4261 ns | 26.0861 ns | 39.0445 ns | 96.63 |    2.26 | 0.1526 |    2600 B |          NA |
