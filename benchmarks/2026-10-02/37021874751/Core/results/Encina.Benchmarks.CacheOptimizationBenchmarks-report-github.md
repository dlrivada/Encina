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
| Cache_TryGetValue_ThenGetOrAdd |    43.4212 ns |   2.9195 ns |  0.1600 ns |  0.946 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4940 ns |   0.0671 ns |  0.0037 ns |  0.272 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3463 ns |   0.0733 ns |  0.0040 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,997.7323 ns | 362.3904 ns | 19.8638 ns | 87.103 |    0.39 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    45.8965 ns |   1.1208 ns |  0.0614 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,222.7448 ns | 100.3709 ns |  5.5017 ns | 70.218 |    0.13 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,109.8220 ns |  69.1539 ns |  3.7906 ns | 89.546 |    0.13 | 0.1526 |    2600 B |          NA |
