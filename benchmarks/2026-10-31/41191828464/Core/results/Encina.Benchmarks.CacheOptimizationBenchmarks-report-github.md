```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    23.4186 ns |    12.1669 ns |   0.6669 ns |   0.940 |    0.03 |      - |         - |          NA |
| TypeCheck_Cached               |     7.5967 ns |     7.2616 ns |   0.3980 ns |   0.305 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2069 ns |     0.1635 ns |   0.0090 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,610.8889 ns |   330.4616 ns |  18.1137 ns | 104.775 |    1.41 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    24.9223 ns |     6.3805 ns |   0.3497 ns |   1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,143.2991 ns |   677.5122 ns |  37.1367 ns |  86.010 |    1.66 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,792.5769 ns | 2,029.9443 ns | 111.2681 ns | 112.066 |    4.10 | 0.1678 |    2832 B |          NA |
