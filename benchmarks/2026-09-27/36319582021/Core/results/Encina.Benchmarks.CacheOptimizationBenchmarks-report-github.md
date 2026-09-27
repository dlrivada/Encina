```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    44.3359 ns |   2.1940 ns |  0.1203 ns |  0.954 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4977 ns |   0.1462 ns |  0.0080 ns |  0.269 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3499 ns |   0.0154 ns |  0.0008 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,908.9688 ns | 111.9314 ns |  6.1353 ns | 84.085 |    0.31 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.4889 ns |   3.3447 ns |  0.1833 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,236.7094 ns | 269.5060 ns | 14.7725 ns | 69.624 |    0.36 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,064.0067 ns |  83.2905 ns |  4.5654 ns | 87.420 |    0.31 | 0.1526 |    2600 B |          NA |
