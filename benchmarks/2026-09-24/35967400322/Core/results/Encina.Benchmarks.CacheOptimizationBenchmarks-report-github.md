```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.4523 ns |   0.2125 ns | 0.0116 ns |  0.946 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.2214 ns |   1.6668 ns | 0.0914 ns |  0.279 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2776 ns |   0.2047 ns | 0.0112 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,000.1584 ns |  35.0105 ns | 1.9190 ns | 91.251 |    0.05 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    43.8369 ns |   0.3178 ns | 0.0174 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,220.8231 ns |  28.3722 ns | 1.5552 ns | 73.473 |    0.04 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,152.8433 ns | 108.5677 ns | 5.9510 ns | 94.734 |    0.12 | 0.1526 |    2600 B |          NA |
