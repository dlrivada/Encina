```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    32.8456 ns |   6.2359 ns |  0.3418 ns |   0.977 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     9.8592 ns |   0.4457 ns |  0.0244 ns |   0.293 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2524 ns |   0.4087 ns |  0.0224 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,658.5732 ns | 412.9435 ns | 22.6348 ns | 108.837 |    0.95 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    33.6166 ns |   4.8825 ns |  0.2676 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,000.8228 ns | 224.5623 ns | 12.3090 ns |  89.270 |    0.69 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,835.9630 ns | 740.0586 ns | 40.5651 ns | 114.114 |    1.31 | 0.1755 |    2936 B |          NA |
