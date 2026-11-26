```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.1455 ns |   4.1221 ns |  0.2259 ns |   0.973 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    13.5014 ns |   2.4141 ns |  0.1323 ns |   0.319 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3705 ns |   0.2558 ns |  0.0140 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,390.4519 ns | 318.7646 ns | 17.4726 ns | 103.811 |    1.81 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.3036 ns |  15.2259 ns |  0.8346 ns |   1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,679.3287 ns | 176.9475 ns |  9.6991 ns |  86.997 |    1.50 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,692.2160 ns | 125.3628 ns |  6.8716 ns | 110.947 |    1.90 | 0.1678 |    2832 B |          NA |
