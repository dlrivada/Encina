```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.9246 ns |   3.8937 ns |  0.2134 ns |   1.009 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4489 ns |   0.1419 ns |  0.0078 ns |   0.299 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2732 ns |   0.0318 ns |  0.0017 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,570.2318 ns | 146.9433 ns |  8.0545 ns | 109.940 |    0.33 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.5704 ns |   2.2909 ns |  0.1256 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,679.5850 ns |  98.0110 ns |  5.3723 ns |  88.515 |    0.26 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,886.6834 ns | 315.4472 ns | 17.2907 ns | 117.553 |    0.47 | 0.1678 |    2832 B |          NA |
