```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.3640 ns |   0.2771 ns |  0.0152 ns |   0.987 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.1289 ns |   0.4562 ns |  0.0250 ns |   0.313 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3503 ns |   0.0065 ns |  0.0004 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,502.2956 ns | 229.9459 ns | 12.6041 ns | 107.478 |    0.33 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.8904 ns |   1.6893 ns |  0.0926 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,618.5273 ns | 312.1382 ns | 17.1093 ns |  86.381 |    0.39 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,851.7183 ns | 317.6445 ns | 17.4112 ns | 115.820 |    0.42 | 0.1678 |    2832 B |          NA |
