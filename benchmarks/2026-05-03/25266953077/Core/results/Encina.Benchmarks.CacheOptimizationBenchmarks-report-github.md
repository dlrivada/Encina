```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.0550 ns |  0.2484 ns |  0.3400 ns |    42.9666 ns |  1.045 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5301 ns |  0.0771 ns |  0.1153 ns |    12.5422 ns |  0.304 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0830 ns |  0.0957 ns |  0.1433 ns |     0.0000 ns |  0.002 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,737.9942 ns | 16.1554 ns | 22.1137 ns | 3,734.7379 ns | 90.724 |    0.90 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    41.2045 ns |  0.2406 ns |  0.3372 ns |    41.1892 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,927.7035 ns | 10.6567 ns | 15.2835 ns | 1,928.5877 ns | 46.787 |    0.52 | 0.0820 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,929.1558 ns | 23.7146 ns | 34.7605 ns | 3,934.7846 ns | 95.364 |    1.13 | 0.1297 |    2248 B |          NA |
