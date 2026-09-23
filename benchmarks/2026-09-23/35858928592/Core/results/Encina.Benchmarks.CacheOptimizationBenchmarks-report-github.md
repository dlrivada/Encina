```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    35.7295 ns |   3.8942 ns |  0.2135 ns |  1.002 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.6505 ns |   3.6476 ns |  0.1999 ns |  0.299 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3060 ns |   0.0252 ns |  0.0014 ns |  0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,993.5012 ns | 150.7550 ns |  8.2634 ns | 83.990 |    1.17 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    35.6471 ns |  10.3472 ns |  0.5672 ns |  1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,523.2649 ns |  70.9069 ns |  3.8866 ns | 42.739 |    0.60 | 0.0820 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,117.9243 ns | 315.0209 ns | 17.2674 ns | 87.481 |    1.27 | 0.1335 |    2248 B |          NA |
