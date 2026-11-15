```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.5117 ns |   5.0500 ns |  0.2768 ns |   0.990 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.4399 ns |   1.5849 ns |  0.0869 ns |   0.320 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3556 ns |   0.3072 ns |  0.0168 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,351.7346 ns | 193.6775 ns | 10.6161 ns | 103.767 |    1.31 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.9434 ns |  11.0520 ns |  0.6058 ns |   1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,620.7173 ns | 283.0532 ns | 15.5151 ns |  86.336 |    1.12 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,719.3687 ns | 654.0840 ns | 35.8525 ns | 112.533 |    1.58 | 0.1678 |    2832 B |          NA |
