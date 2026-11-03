```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.0243 ns |   4.4463 ns |  0.2437 ns |  0.891 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.8744 ns |   5.2833 ns |  0.2896 ns |  0.280 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3052 ns |   0.0354 ns |  0.0019 ns |  0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,308.1697 ns | 109.3257 ns |  5.9925 ns | 71.823 |    0.16 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    46.0604 ns |   1.6182 ns |  0.0887 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,852.6177 ns | 301.8267 ns | 16.5441 ns | 61.932 |    0.33 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,473.0999 ns | 225.5126 ns | 12.3611 ns | 75.403 |    0.26 | 0.0305 |    2832 B |          NA |
