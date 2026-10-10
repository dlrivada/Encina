```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.0499 ns |     0.8543 ns |  0.0468 ns |   0.993 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.6086 ns |     3.4484 ns |  0.1890 ns |   0.298 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3586 ns |     0.1681 ns |  0.0092 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,246.2811 ns |   256.4286 ns | 14.0557 ns | 100.250 |    0.34 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    42.3572 ns |     1.5801 ns |  0.0866 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,639.7440 ns |   759.4520 ns | 41.6281 ns |  85.930 |    0.86 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,355.9317 ns | 1,027.6661 ns | 56.3298 ns | 102.838 |    1.17 | 0.1755 |    2936 B |          NA |
