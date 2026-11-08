```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.1614 ns |  0.1782 ns |  0.2611 ns |   1.017 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4468 ns |  0.0061 ns |  0.0086 ns |   0.300 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2731 ns |  0.0014 ns |  0.0020 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,396.5296 ns | 30.8696 ns | 45.2483 ns | 106.101 |    1.10 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.4374 ns |  0.0622 ns |  0.0892 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,620.1537 ns | 30.1915 ns | 44.2544 ns |  87.365 |    1.07 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,782.5564 ns | 12.1093 ns | 17.3667 ns | 115.417 |    0.48 | 0.1678 |    2832 B |          NA |
