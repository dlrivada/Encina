```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.1916 ns |   2.1130 ns |  0.1158 ns |   1.016 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4606 ns |   0.1819 ns |  0.0100 ns |   0.300 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2708 ns |   0.0476 ns |  0.0026 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,509.9269 ns | 255.6928 ns | 14.0154 ns | 108.638 |    0.31 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.5132 ns |   0.7060 ns |  0.0387 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,657.5853 ns |  55.6629 ns |  3.0511 ns |  88.107 |    0.10 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,746.9322 ns | 245.2744 ns | 13.4443 ns | 114.347 |    0.30 | 0.1678 |    2832 B |          NA |
