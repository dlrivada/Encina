```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    35.4681 ns |   8.3500 ns |  0.4577 ns |   1.060 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.2840 ns |   3.0778 ns |  0.1687 ns |   0.307 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2288 ns |   0.0444 ns |  0.0024 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,535.2520 ns |  30.5362 ns |  1.6738 ns | 105.647 |    0.37 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.4633 ns |   2.4696 ns |  0.1354 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,890.3418 ns |  76.9436 ns |  4.2175 ns |  86.374 |    0.32 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,690.4021 ns | 270.1891 ns | 14.8100 ns | 110.283 |    0.54 | 0.1678 |    2832 B |          NA |
