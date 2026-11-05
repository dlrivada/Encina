```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    44.9383 ns |   7.2421 ns | 0.3970 ns |   1.067 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4909 ns |   1.2297 ns | 0.0674 ns |   0.297 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2735 ns |   0.0832 ns | 0.0046 ns |   0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,464.0982 ns |  95.4459 ns | 5.2317 ns | 105.980 |    0.34 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.1224 ns |   2.6909 ns | 0.1475 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,588.6685 ns |  59.6712 ns | 3.2708 ns |  85.197 |    0.27 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,590.9614 ns | 117.8082 ns | 6.4575 ns | 108.992 |    0.36 | 0.1678 |    2832 B |          NA |
