```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.5991 ns |     0.9264 ns |   0.0508 ns |   0.995 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.5673 ns |     2.4140 ns |   0.1323 ns |   0.313 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0203 ns |     0.1044 ns |   0.0057 ns |   0.001 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,617.9808 ns |   419.3198 ns |  22.9843 ns | 107.184 |    0.83 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.7557 ns |     3.8461 ns |   0.2108 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,938.7528 ns |   140.6060 ns |   7.7071 ns |  87.062 |    0.51 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,826.2132 ns | 1,838.2346 ns | 100.7598 ns | 113.353 |    2.66 | 0.1678 |    2832 B |          NA |
