```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.8953 ns |     0.3461 ns |  0.0190 ns |   1.026 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.7902 ns |     4.8194 ns |  0.2642 ns |   0.327 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3046 ns |     0.2065 ns |  0.0113 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,574.7144 ns | 1,208.7380 ns | 66.2550 ns | 108.201 |    1.77 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.0379 ns |     2.1820 ns |  0.1196 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,937.6070 ns |    98.3985 ns |  5.3936 ns |  88.917 |    0.31 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,693.7201 ns |   173.0964 ns |  9.4880 ns | 111.804 |    0.43 | 0.1678 |    2832 B |          NA |
