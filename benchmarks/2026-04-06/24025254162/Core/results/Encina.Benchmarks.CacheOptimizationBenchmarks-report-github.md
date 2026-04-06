```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_GetOrAdd_Direct          |    44.2210 ns |   3.2852 ns |  0.1801 ns |  1.000 |    0.00 |      - |         - |          NA |
| Cache_TryGetValue_ThenGetOrAdd |    43.4817 ns |   7.6299 ns |  0.4182 ns |  0.983 |    0.01 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,627.5854 ns | 751.0341 ns | 41.1667 ns | 82.034 |    0.86 | 0.1335 |    2272 B |          NA |
| Send_Query_CacheHit            | 3,734.0140 ns | 510.0391 ns | 27.9570 ns | 84.441 |    0.62 | 0.1335 |    2248 B |          NA |
| Publish_Notification_CacheHit  | 1,925.8249 ns | 193.8137 ns | 10.6236 ns | 43.550 |    0.26 | 0.0801 |    1384 B |          NA |
| TypeCheck_Cached               |    12.1359 ns |   0.1514 ns |  0.0083 ns |  0.274 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2737 ns |   0.0485 ns |  0.0027 ns |  0.006 |    0.00 |      - |         - |          NA |
