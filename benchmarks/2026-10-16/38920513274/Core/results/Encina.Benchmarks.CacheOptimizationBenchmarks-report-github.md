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
| Cache_TryGetValue_ThenGetOrAdd |    37.3571 ns |  19.1428 ns |  1.0493 ns |   1.106 |    0.03 |      - |         - |          NA |
| TypeCheck_Cached               |    10.4547 ns |  14.2339 ns |  0.7802 ns |   0.309 |    0.02 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2183 ns |   0.0228 ns |  0.0013 ns |   0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,634.4855 ns | 528.8380 ns | 28.9874 ns | 107.595 |    0.78 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    33.7795 ns |   1.5273 ns |  0.0837 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,974.3887 ns | 277.9149 ns | 15.2335 ns |  88.053 |    0.43 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,721.3299 ns | 375.3527 ns | 20.5743 ns | 110.166 |    0.58 | 0.1755 |    2936 B |          NA |
