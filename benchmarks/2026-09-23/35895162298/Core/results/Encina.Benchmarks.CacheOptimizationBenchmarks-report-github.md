```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    40.4287 ns |   4.4069 ns |  0.2416 ns |  0.956 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.2394 ns |   3.8478 ns |  0.2109 ns |  0.289 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2744 ns |   0.0141 ns |  0.0008 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,605.5162 ns | 100.7704 ns |  5.5236 ns | 85.236 |    0.19 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    42.3005 ns |   1.5705 ns |  0.0861 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,953.4664 ns | 211.9545 ns | 11.6179 ns | 46.181 |    0.25 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,718.6198 ns | 135.6781 ns |  7.4370 ns | 87.910 |    0.22 | 0.1335 |    2248 B |          NA |
