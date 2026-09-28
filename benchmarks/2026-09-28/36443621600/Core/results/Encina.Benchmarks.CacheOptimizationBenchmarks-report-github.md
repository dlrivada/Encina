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
| Cache_TryGetValue_ThenGetOrAdd |    43.6712 ns |   2.8614 ns |  0.1568 ns |  0.926 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4999 ns |   0.1576 ns |  0.0086 ns |  0.265 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3499 ns |   0.0069 ns |  0.0004 ns |  0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,957.9115 ns | 397.7201 ns | 21.8004 ns | 83.957 |    0.55 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    47.1430 ns |   4.4517 ns |  0.2440 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,211.4550 ns | 352.7484 ns | 19.3353 ns | 68.123 |    0.47 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,164.6178 ns | 137.9923 ns |  7.5638 ns | 88.342 |    0.42 | 0.1526 |    2600 B |          NA |
