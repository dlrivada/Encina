```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.75GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.8347 ns |   0.0828 ns |  0.0045 ns |   1.009 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5067 ns |   0.1793 ns |  0.0098 ns |   0.302 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3530 ns |   0.0518 ns |  0.0028 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,380.1218 ns | 152.5569 ns |  8.3622 ns | 105.636 |    0.24 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    41.4643 ns |   1.3463 ns |  0.0738 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,599.8258 ns | 349.1527 ns | 19.1382 ns |  86.818 |    0.42 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,478.8724 ns | 143.1139 ns |  7.8446 ns | 108.018 |    0.23 | 0.1755 |    2936 B |          NA |
