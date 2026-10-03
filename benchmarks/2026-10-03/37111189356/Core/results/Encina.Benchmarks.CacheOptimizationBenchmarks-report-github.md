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
| Cache_TryGetValue_ThenGetOrAdd |    46.5677 ns |   1.2572 ns | 0.0689 ns |  1.018 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1516 ns |   0.3023 ns | 0.0166 ns |  0.266 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2733 ns |   0.0135 ns | 0.0007 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,984.5627 ns |  39.4234 ns | 2.1609 ns | 87.102 |    0.13 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    45.7458 ns |   1.3704 ns | 0.0751 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,238.9430 ns | 109.0699 ns | 5.9785 ns | 70.803 |    0.15 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,094.9063 ns |  87.5859 ns | 4.8009 ns | 89.515 |    0.16 | 0.1526 |    2600 B |          NA |
