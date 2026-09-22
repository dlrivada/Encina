```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.0292 ns |   2.1197 ns |  0.1162 ns |  0.971 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1320 ns |   0.1050 ns |  0.0058 ns |  0.287 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2714 ns |   0.0144 ns |  0.0008 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,669.0997 ns | 849.4860 ns | 46.5632 ns | 86.866 |    0.95 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    42.2385 ns |   0.1675 ns |  0.0092 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,022.2248 ns |  23.3196 ns |  1.2782 ns | 47.876 |    0.03 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,753.8608 ns | 145.7714 ns |  7.9902 ns | 88.873 |    0.16 | 0.1335 |    2248 B |          NA |
