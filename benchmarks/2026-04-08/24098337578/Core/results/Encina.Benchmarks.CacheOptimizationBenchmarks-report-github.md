```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.9873 ns |  0.2380 ns |  0.3413 ns |    43.0178 ns |  0.972 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5601 ns |  0.1800 ns |  0.2638 ns |    12.5136 ns |  0.284 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1779 ns |  0.1215 ns |  0.1663 ns |     0.3275 ns |  0.004 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,829.9041 ns | 12.8259 ns | 18.8000 ns | 3,831.8531 ns | 86.598 |    1.54 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    44.2390 ns |  0.5484 ns |  0.7687 ns |    44.6989 ns |  1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,970.7827 ns | 13.7766 ns | 19.7580 ns | 1,969.8721 ns | 44.561 |    0.88 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,972.9252 ns | 14.6266 ns | 20.0210 ns | 3,969.1618 ns | 89.832 |    1.60 | 0.1297 |    2248 B |          NA |
