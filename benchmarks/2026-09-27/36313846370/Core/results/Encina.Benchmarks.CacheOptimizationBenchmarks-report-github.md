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
| Cache_TryGetValue_ThenGetOrAdd |    43.9911 ns |   4.1362 ns |  0.2267 ns |  0.945 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5019 ns |   0.3541 ns |  0.0194 ns |  0.269 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3516 ns |   0.0065 ns |  0.0004 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,964.8759 ns | 677.7459 ns | 37.1495 ns | 85.196 |    0.70 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.5384 ns |   1.5858 ns |  0.0869 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,147.5547 ns | 183.1157 ns | 10.0372 ns | 67.634 |    0.22 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 3,979.4791 ns | 203.5225 ns | 11.1558 ns | 85.510 |    0.25 | 0.1526 |    2600 B |          NA |
