```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.9293 ns |   0.9084 ns |  0.0498 ns |   1.011 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5554 ns |   1.0439 ns |  0.0572 ns |   0.303 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2774 ns |   0.0894 ns |  0.0049 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,458.4879 ns | 342.5487 ns | 18.7762 ns | 107.547 |    0.39 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.4561 ns |   0.1312 ns |  0.0072 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,553.9952 ns | 105.4187 ns |  5.7784 ns |  85.729 |    0.12 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,840.9178 ns | 294.6018 ns | 16.1481 ns | 116.772 |    0.34 | 0.1678 |    2832 B |          NA |
