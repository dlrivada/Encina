```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.4496 ns |     7.1830 ns |   0.3937 ns |   1.026 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4588 ns |     0.3014 ns |   0.0165 ns |   0.301 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2705 ns |     0.0116 ns |   0.0006 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,568.8998 ns |   404.2884 ns |  22.1604 ns | 110.477 |    0.48 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.3562 ns |     1.1090 ns |   0.0608 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,536.8977 ns |    89.6317 ns |   4.9130 ns |  85.523 |    0.15 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,673.9196 ns | 2,055.0634 ns | 112.6450 ns | 113.016 |    2.36 | 0.1678 |    2832 B |          NA |
