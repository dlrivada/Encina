```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.4836 ns |   3.1429 ns |  0.1723 ns |  0.923 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1458 ns |   0.2653 ns |  0.0145 ns |  0.270 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2708 ns |   0.0084 ns |  0.0005 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,111.5841 ns | 221.2563 ns | 12.1278 ns | 91.434 |    0.26 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    44.9677 ns |   1.1306 ns |  0.0620 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,311.3263 ns | 130.0648 ns |  7.1293 ns | 73.638 |    0.16 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,163.6154 ns | 191.5554 ns | 10.4998 ns | 92.591 |    0.23 | 0.1526 |    2600 B |          NA |
