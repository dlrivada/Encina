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
| Cache_TryGetValue_ThenGetOrAdd |    45.4393 ns |  18.1110 ns | 0.9927 ns |  1.038 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1410 ns |   0.6029 ns | 0.0330 ns |  0.277 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2731 ns |   0.0516 ns | 0.0028 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,707.6011 ns | 174.8359 ns | 9.5833 ns | 84.709 |    0.21 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    43.7687 ns |   0.9648 ns | 0.0529 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,003.5111 ns | 110.8743 ns | 6.0774 ns | 45.775 |    0.13 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,816.4113 ns |  47.0434 ns | 2.5786 ns | 87.195 |    0.10 | 0.1335 |    2248 B |          NA |
