```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.1930 ns |   1.9200 ns | 0.1052 ns |   1.022 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5188 ns |   0.1785 ns | 0.0098 ns |   0.303 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3523 ns |   0.0197 ns | 0.0011 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,295.6264 ns |  17.5713 ns | 0.9631 ns | 104.031 |    0.03 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    41.2918 ns |   0.2032 ns | 0.0111 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,587.2658 ns | 120.5604 ns | 6.6083 ns |  86.876 |    0.14 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,485.7330 ns | 130.8042 ns | 7.1698 ns | 108.635 |    0.15 | 0.1755 |    2936 B |          NA |
