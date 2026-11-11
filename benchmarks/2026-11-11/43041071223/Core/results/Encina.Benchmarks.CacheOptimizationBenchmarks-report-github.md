```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    38.3829 ns |    13.0150 ns |   0.7134 ns |  0.928 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    12.0701 ns |     3.7875 ns |   0.2076 ns |  0.292 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2048 ns |     0.5113 ns |   0.0280 ns |  0.005 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,151.0313 ns |   657.4643 ns |  36.0378 ns | 76.152 |    0.93 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.3800 ns |     6.2416 ns |   0.3421 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,626.0260 ns | 1,346.2160 ns |  73.7906 ns | 63.464 |    1.61 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,355.0100 ns | 2,334.6465 ns | 127.9699 ns | 81.082 |    2.74 | 0.0305 |    2832 B |          NA |
