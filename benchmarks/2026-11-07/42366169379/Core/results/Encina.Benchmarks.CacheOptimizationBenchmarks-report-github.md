```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    32.9666 ns |     2.7222 ns |  0.1492 ns |   0.92 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    11.3296 ns |     3.5392 ns |  0.1940 ns |   0.32 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3846 ns |     1.0949 ns |  0.0600 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,926.2977 ns |   260.5141 ns | 14.2797 ns | 110.00 |    1.92 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    35.7032 ns |    12.8295 ns |  0.7032 ns |   1.00 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,018.8105 ns |   864.5082 ns | 47.3866 ns |  84.58 |    1.85 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,823.3643 ns | 1,514.3076 ns | 83.0043 ns | 107.12 |    2.73 | 0.1678 |    2832 B |          NA |
