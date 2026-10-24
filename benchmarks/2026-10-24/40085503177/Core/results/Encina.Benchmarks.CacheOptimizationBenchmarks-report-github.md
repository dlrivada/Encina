```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.1990 ns |   3.9855 ns |  0.2185 ns |   1.010 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4799 ns |   0.0969 ns |  0.0053 ns |   0.299 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2751 ns |   0.0992 ns |  0.0054 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,504.1247 ns |  27.0609 ns |  1.4833 ns | 107.801 |    0.15 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.7817 ns |   1.2046 ns |  0.0660 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,606.7603 ns | 156.6862 ns |  8.5885 ns |  86.324 |    0.21 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,686.7367 ns | 300.5542 ns | 16.4744 ns | 112.172 |    0.37 | 0.1678 |    2832 B |          NA |
