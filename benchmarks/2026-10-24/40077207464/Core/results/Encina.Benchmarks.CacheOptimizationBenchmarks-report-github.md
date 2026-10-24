```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.2492 ns |   0.7750 ns | 0.0425 ns |   0.961 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.1336 ns |   0.0993 ns | 0.0054 ns |   0.306 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3499 ns |   0.0233 ns | 0.0013 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,541.1229 ns |  84.7280 ns | 4.6442 ns | 105.844 |    0.51 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.9047 ns |   4.3252 ns | 0.2371 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,603.5197 ns | 144.0274 ns | 7.8946 ns |  83.991 |    0.43 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,780.4236 ns | 178.7376 ns | 9.7972 ns | 111.422 |    0.57 | 0.1678 |    2832 B |          NA |
