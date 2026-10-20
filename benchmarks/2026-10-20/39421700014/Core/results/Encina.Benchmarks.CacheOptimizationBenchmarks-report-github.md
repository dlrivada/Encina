```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.3068 ns |   0.8405 ns |  0.0461 ns |   0.969 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.1907 ns |   0.7953 ns |  0.0436 ns |   0.310 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3527 ns |   0.0627 ns |  0.0034 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,490.0092 ns | 116.7674 ns |  6.4004 ns | 105.374 |    0.21 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.6105 ns |   1.3883 ns |  0.0761 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,563.0663 ns | 127.8176 ns |  7.0061 ns |  83.620 |    0.19 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,776.8484 ns | 207.3758 ns | 11.3670 ns | 112.105 |    0.29 | 0.1678 |    2832 B |          NA |
