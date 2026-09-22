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
| Cache_TryGetValue_ThenGetOrAdd |    40.2882 ns |   2.1396 ns | 0.1173 ns |  0.882 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1472 ns |   0.1320 ns | 0.0072 ns |  0.266 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2877 ns |   0.2485 ns | 0.0136 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,644.8631 ns | 102.0699 ns | 5.5948 ns | 79.836 |    0.13 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    45.6547 ns |   0.8228 ns | 0.0451 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,998.9009 ns | 174.0646 ns | 9.5411 ns | 43.783 |    0.18 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,800.8473 ns | 145.5041 ns | 7.9756 ns | 83.252 |    0.17 | 0.1335 |    2248 B |          NA |
