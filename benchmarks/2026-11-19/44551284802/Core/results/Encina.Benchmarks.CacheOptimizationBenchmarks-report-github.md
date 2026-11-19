```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.2955 ns |     8.9154 ns |  0.4887 ns |   1.016 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.4632 ns |     0.8234 ns |  0.0451 ns |   0.323 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3600 ns |     0.3409 ns |  0.0187 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,435.8760 ns | 1,073.4589 ns | 58.8399 ns | 106.529 |    1.30 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.6407 ns |     3.6065 ns |  0.1977 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,463.4587 ns |   145.6729 ns |  7.9848 ns |  83.176 |    0.38 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,609.3200 ns |   186.2578 ns | 10.2094 ns | 110.694 |    0.50 | 0.1678 |    2832 B |          NA |
