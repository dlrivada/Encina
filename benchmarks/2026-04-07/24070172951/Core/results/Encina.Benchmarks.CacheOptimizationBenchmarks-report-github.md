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
| Cache_TryGetValue_ThenGetOrAdd |    43.5457 ns |   0.3610 ns |  0.0198 ns |  0.958 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.2484 ns |   1.6748 ns |  0.0918 ns |  0.269 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2692 ns |   0.0186 ns |  0.0010 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,597.2897 ns |  71.0478 ns |  3.8944 ns | 79.149 |    0.49 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    45.4508 ns |   5.7886 ns |  0.3173 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,953.0099 ns |  66.8191 ns |  3.6626 ns | 42.971 |    0.27 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,856.8460 ns | 430.2923 ns | 23.5858 ns | 84.860 |    0.68 | 0.1297 |    2248 B |          NA |
