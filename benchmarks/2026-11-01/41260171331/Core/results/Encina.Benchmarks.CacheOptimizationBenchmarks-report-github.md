```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    23.4156 ns |  0.4699 ns |  0.6888 ns |   0.96 |    0.04 |      - |         - |          NA |
| TypeCheck_Cached               |     6.9966 ns |  0.1058 ns |  0.1584 ns |   0.29 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2500 ns |  0.0316 ns |  0.0462 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,524.2481 ns | 22.6627 ns | 33.2187 ns | 103.43 |    2.87 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    24.4195 ns |  0.4196 ns |  0.6018 ns |   1.00 |    0.03 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,092.0866 ns | 21.1304 ns | 30.3046 ns |  85.72 |    2.43 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,662.5091 ns | 20.8549 ns | 27.8407 ns | 109.10 |    2.90 | 0.1678 |    2832 B |          NA |
