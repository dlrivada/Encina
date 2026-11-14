```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.6713 ns |   6.4105 ns |  0.3514 ns |   0.995 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.1275 ns |   0.3183 ns |  0.0174 ns |   0.313 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3561 ns |   0.1217 ns |  0.0067 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,475.7886 ns | 189.5104 ns | 10.3877 ns | 106.863 |    0.26 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.8835 ns |   1.2266 ns |  0.0672 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,538.0442 ns |  56.5941 ns |  3.1021 ns |  84.474 |    0.13 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,681.7212 ns | 248.5309 ns | 13.6228 ns | 111.780 |    0.32 | 0.1678 |    2832 B |          NA |
