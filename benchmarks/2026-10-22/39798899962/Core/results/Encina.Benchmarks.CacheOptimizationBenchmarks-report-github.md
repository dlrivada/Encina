```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.3528 ns |   3.8515 ns |  0.2111 ns |   0.992 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.1353 ns |   0.1439 ns |  0.0079 ns |   0.315 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3506 ns |   0.0210 ns |  0.0012 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,559.6507 ns | 391.4014 ns | 21.4540 ns | 109.384 |    0.55 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.6852 ns |   2.5772 ns |  0.1413 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,631.1953 ns |  99.0883 ns |  5.4314 ns |  87.111 |    0.28 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,795.1697 ns | 144.2734 ns |  7.9081 ns | 115.034 |    0.37 | 0.1678 |    2832 B |          NA |
