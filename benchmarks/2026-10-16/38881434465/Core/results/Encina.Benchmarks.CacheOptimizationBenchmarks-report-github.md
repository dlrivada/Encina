```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    22.5879 ns |   2.1123 ns |  0.1158 ns |   0.91 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     7.1076 ns |   1.3440 ns |  0.0737 ns |   0.29 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2496 ns |   0.3867 ns |  0.0212 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,641.6032 ns | 182.7883 ns | 10.0192 ns | 106.43 |    0.74 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    24.8220 ns |   3.1860 ns |  0.1746 ns |   1.00 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,094.5864 ns | 346.2731 ns | 18.9804 ns |  84.39 |    0.84 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,682.3232 ns | 780.7620 ns | 42.7962 ns | 108.07 |    1.63 | 0.1755 |    2936 B |          NA |
