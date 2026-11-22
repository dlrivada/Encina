```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.1706 ns |  0.1175 ns |  0.1527 ns |    41.2212 ns |   0.983 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.5332 ns |  0.1230 ns |  0.1724 ns |    13.4464 ns |   0.323 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3644 ns |  0.0092 ns |  0.0135 ns |     0.3611 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,547.4482 ns | 27.2512 ns | 39.0828 ns | 4,536.8404 ns | 108.566 |    1.40 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.8903 ns |  0.2868 ns |  0.4204 ns |    41.7768 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,652.9823 ns | 28.8258 ns | 42.2525 ns | 3,657.0907 ns |  87.212 |    1.31 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,740.2124 ns | 51.6433 ns | 70.6899 ns | 4,692.8035 ns | 113.169 |    1.99 | 0.1678 |    2832 B |          NA |
