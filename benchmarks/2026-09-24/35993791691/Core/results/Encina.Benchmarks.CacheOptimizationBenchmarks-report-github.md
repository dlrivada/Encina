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
| Cache_TryGetValue_ThenGetOrAdd |    40.8344 ns |   5.0340 ns |  0.2759 ns |  0.938 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1553 ns |   0.6659 ns |  0.0365 ns |  0.279 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2711 ns |   0.0116 ns |  0.0006 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,939.5535 ns |  85.5754 ns |  4.6907 ns | 90.539 |    0.38 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    43.5129 ns |   3.7677 ns |  0.2065 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,309.8229 ns | 141.1324 ns |  7.7359 ns | 76.066 |    0.35 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,129.3603 ns | 205.8468 ns | 11.2832 ns | 94.901 |    0.45 | 0.1526 |    2600 B |          NA |
