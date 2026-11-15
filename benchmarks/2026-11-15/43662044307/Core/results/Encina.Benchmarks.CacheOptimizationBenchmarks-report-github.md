```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    24.9848 ns |  0.2629 ns |  0.3935 ns |   0.978 |    0.03 |      - |         - |          NA |
| TypeCheck_Cached               |     7.7126 ns |  0.1563 ns |  0.2291 ns |   0.302 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2241 ns |  0.0231 ns |  0.0338 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,626.3517 ns | 31.4983 ns | 45.1739 ns | 102.765 |    3.22 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    25.5748 ns |  0.4610 ns |  0.6900 ns |   1.001 |    0.04 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,149.8270 ns | 23.9275 ns | 35.8136 ns |  84.119 |    2.61 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,692.5490 ns | 36.3063 ns | 52.0695 ns | 105.355 |    3.42 | 0.1678 |    2832 B |          NA |
