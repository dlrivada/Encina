```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.2209 ns |   1.0766 ns |  0.0590 ns |   0.982 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.1518 ns |   1.0430 ns |  0.0572 ns |   0.313 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3501 ns |   0.0429 ns |  0.0024 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,432.7143 ns | 391.0239 ns | 21.4333 ns | 105.636 |    0.80 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.9635 ns |   5.6163 ns |  0.3078 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,561.7967 ns |  87.3747 ns |  4.7893 ns |  84.881 |    0.55 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,601.1928 ns | 261.0579 ns | 14.3095 ns | 109.651 |    0.75 | 0.1678 |    2832 B |          NA |
