```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Median        | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    23.4908 ns |   5.0595 ns |  0.2773 ns |    23.5361 ns |   0.924 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     7.3853 ns |   4.0147 ns |  0.2201 ns |     7.2799 ns |   0.291 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1653 ns |   1.5455 ns |  0.0847 ns |     0.1227 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,480.0910 ns | 178.4580 ns |  9.7819 ns | 2,485.3766 ns |  97.577 |    0.70 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    25.4176 ns |   3.4017 ns |  0.1865 ns |    25.4032 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,097.5074 ns | 135.5108 ns |  7.4278 ns | 2,093.4265 ns |  82.525 |    0.58 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,617.9571 ns | 236.1202 ns | 12.9425 ns | 2,618.2660 ns | 103.002 |    0.79 | 0.1755 |    2936 B |          NA |
