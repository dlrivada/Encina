```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    30.8186 ns |   0.7703 ns |  0.0422 ns |  0.945 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.3799 ns |   0.3469 ns |  0.0190 ns |  0.318 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1891 ns |   0.0094 ns |  0.0005 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,503.6310 ns | 362.6951 ns | 19.8805 ns | 76.806 |    0.54 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.5969 ns |   1.0434 ns |  0.0572 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,998.1137 ns | 118.1644 ns |  6.4770 ns | 61.298 |    0.20 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,566.4092 ns |  82.0795 ns |  4.4991 ns | 78.732 |    0.17 | 0.0305 |    2832 B |          NA |
