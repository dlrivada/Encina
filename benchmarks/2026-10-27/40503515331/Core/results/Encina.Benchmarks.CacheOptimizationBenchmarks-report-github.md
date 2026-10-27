```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.3848 ns |     4.3074 ns |   0.2361 ns |   1.021 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.6968 ns |     6.8111 ns |   0.3733 ns |   0.306 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2766 ns |     0.1510 ns |   0.0083 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,641.1388 ns |   900.1897 ns |  49.3424 ns | 111.765 |    1.03 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.5258 ns |     0.5019 ns |   0.0275 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,583.2299 ns |   524.5182 ns |  28.7506 ns |  86.289 |    0.60 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,697.4588 ns | 1,858.6075 ns | 101.8765 ns | 113.121 |    2.13 | 0.1678 |    2832 B |          NA |
