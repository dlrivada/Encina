```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.1395 ns |   1.9045 ns |  0.1044 ns |   1.010 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5442 ns |   1.0032 ns |  0.0550 ns |   0.301 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3550 ns |   0.0629 ns |  0.0034 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,389.9570 ns | 325.5812 ns | 17.8462 ns | 105.190 |    0.37 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    41.7335 ns |   0.3296 ns |  0.0181 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,601.7726 ns |  96.0986 ns |  5.2675 ns |  86.304 |    0.11 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,490.5785 ns | 244.5576 ns | 13.4050 ns | 107.601 |    0.28 | 0.1755 |    2936 B |          NA |
