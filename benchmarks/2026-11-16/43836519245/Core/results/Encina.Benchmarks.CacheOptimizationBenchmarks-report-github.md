```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.3434 ns |   0.7226 ns |  0.0396 ns |   0.990 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.5065 ns |   1.7825 ns |  0.0977 ns |   0.324 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3337 ns |   0.1037 ns |  0.0057 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,606.4877 ns | 135.2563 ns |  7.4139 ns | 110.351 |    0.31 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.7440 ns |   2.1692 ns |  0.1189 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,676.1104 ns | 203.5748 ns | 11.1586 ns |  88.064 |    0.32 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,848.7237 ns | 611.8394 ns | 33.5370 ns | 116.154 |    0.75 | 0.1678 |    2832 B |          NA |
