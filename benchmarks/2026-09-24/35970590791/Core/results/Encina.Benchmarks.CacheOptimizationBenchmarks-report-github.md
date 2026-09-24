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
| Cache_TryGetValue_ThenGetOrAdd |    40.8730 ns |   3.5690 ns |  0.1956 ns |  0.939 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1837 ns |   0.6451 ns |  0.0354 ns |  0.280 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2727 ns |   0.0120 ns |  0.0007 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,768.1983 ns | 113.6427 ns |  6.2291 ns | 86.529 |    0.13 | 0.1564 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    43.5483 ns |   0.5494 ns |  0.0301 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,172.6339 ns | 126.3897 ns |  6.9278 ns | 72.853 |    0.14 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,026.3084 ns | 335.5526 ns | 18.3928 ns | 92.456 |    0.37 | 0.1526 |    2600 B |          NA |
