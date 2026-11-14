```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.3476 ns |   0.5097 ns |  0.0279 ns |   0.986 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.5723 ns |   2.9088 ns |  0.1594 ns |   0.324 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3634 ns |   0.0603 ns |  0.0033 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,383.9722 ns |  88.9033 ns |  4.8731 ns | 104.520 |    0.67 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.9453 ns |   5.6492 ns |  0.3097 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,588.7673 ns | 415.0997 ns | 22.7530 ns |  85.561 |    0.72 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,718.0496 ns | 900.7312 ns | 49.3721 ns | 112.485 |    1.25 | 0.1678 |    2832 B |          NA |
