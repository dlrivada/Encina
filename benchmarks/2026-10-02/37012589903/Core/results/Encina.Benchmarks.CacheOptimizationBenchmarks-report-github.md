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
| Cache_TryGetValue_ThenGetOrAdd |    41.4756 ns |   0.8542 ns |  0.0468 ns |  0.889 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5003 ns |   0.2454 ns |  0.0135 ns |  0.268 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3518 ns |   0.0710 ns |  0.0039 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,068.8352 ns | 320.9711 ns | 17.5935 ns | 87.179 |    0.36 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.6722 ns |   1.7199 ns |  0.0943 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,247.5341 ns |  96.9329 ns |  5.3132 ns | 69.582 |    0.16 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,018.2027 ns | 260.8686 ns | 14.2991 ns | 86.094 |    0.31 | 0.1526 |    2600 B |          NA |
