```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.6461 ns |   3.6056 ns |  0.1976 ns |   1.005 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4684 ns |   0.2525 ns |  0.0138 ns |   0.301 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0000 ns |   0.0000 ns |  0.0000 ns |   0.000 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,508.4533 ns | 133.0520 ns |  7.2930 ns | 108.774 |    0.16 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.4480 ns |   0.2845 ns |  0.0156 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,578.2050 ns | 226.1930 ns | 12.3984 ns |  86.330 |    0.26 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,708.8129 ns | 350.7014 ns | 19.2231 ns | 113.608 |    0.40 | 0.1678 |    2832 B |          NA |
