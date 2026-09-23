```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    40.6134 ns |   0.2882 ns |  0.0158 ns |  0.936 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1686 ns |   1.1183 ns |  0.0613 ns |  0.280 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2717 ns |   0.0038 ns |  0.0002 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,815.6239 ns |   8.8558 ns |  0.4854 ns | 87.943 |    0.39 | 0.1564 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    43.3883 ns |   4.0510 ns |  0.2221 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,218.3083 ns | 112.3519 ns |  6.1584 ns | 74.176 |    0.35 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 3,995.5771 ns | 552.7929 ns | 30.3004 ns | 92.090 |    0.73 | 0.1526 |    2600 B |          NA |
