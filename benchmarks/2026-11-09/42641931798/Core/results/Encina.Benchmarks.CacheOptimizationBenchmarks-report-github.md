```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.8933 ns |  15.2634 ns |  0.8366 ns |   1.012 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4647 ns |   0.8745 ns |  0.0479 ns |   0.301 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2737 ns |   0.0093 ns |  0.0005 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,516.5430 ns | 287.7858 ns | 15.7745 ns | 109.060 |    0.33 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.4132 ns |   0.0438 ns |  0.0024 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,508.7474 ns | 224.9758 ns | 12.3317 ns |  84.725 |    0.26 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,574.9851 ns | 301.5216 ns | 16.5274 ns | 110.472 |    0.35 | 0.1678 |    2832 B |          NA |
