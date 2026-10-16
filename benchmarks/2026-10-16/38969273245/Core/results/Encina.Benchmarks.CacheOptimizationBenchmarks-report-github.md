```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    23.8685 ns |  10.8451 ns |  0.5945 ns |   0.95 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     7.4742 ns |   3.6066 ns |  0.1977 ns |   0.30 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2681 ns |   1.2820 ns |  0.0703 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,592.3731 ns | 302.6020 ns | 16.5866 ns | 103.65 |    0.99 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    25.0131 ns |   4.0735 ns |  0.2233 ns |   1.00 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,123.3198 ns | 275.5522 ns | 15.1039 ns |  84.89 |    0.84 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,737.7633 ns | 504.6696 ns | 27.6626 ns | 109.46 |    1.28 | 0.1678 |    2832 B |          NA |
