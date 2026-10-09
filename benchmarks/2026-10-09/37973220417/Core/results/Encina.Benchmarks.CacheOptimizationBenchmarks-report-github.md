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
| Cache_TryGetValue_ThenGetOrAdd |    43.5846 ns |   6.5081 ns |  0.3567 ns |   1.055 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.3611 ns |   0.4375 ns |  0.0240 ns |   0.299 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3641 ns |   0.1565 ns |  0.0086 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,480.7240 ns | 371.7784 ns | 20.3784 ns | 108.433 |    0.43 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    41.3225 ns |   0.0600 ns |  0.0033 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,728.7731 ns | 260.7047 ns | 14.2901 ns |  90.236 |    0.30 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,557.4635 ns | 398.2336 ns | 21.8285 ns | 110.290 |    0.46 | 0.1755 |    2936 B |          NA |
