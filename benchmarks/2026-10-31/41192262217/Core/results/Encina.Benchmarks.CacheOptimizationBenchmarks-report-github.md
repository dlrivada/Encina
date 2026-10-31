```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.0059 ns |    11.4994 ns |  0.6303 ns |   1.011 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.9202 ns |     4.3165 ns |  0.2366 ns |   0.311 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2665 ns |     0.0550 ns |  0.0030 ns |   0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,596.5576 ns | 1,079.8769 ns | 59.1917 ns | 110.665 |    1.24 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.5356 ns |     0.4439 ns |  0.0243 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,585.1871 ns |   493.6704 ns | 27.0597 ns |  86.316 |    0.57 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,770.0339 ns |   158.0875 ns |  8.6653 ns | 114.842 |    0.19 | 0.1678 |    2832 B |          NA |
