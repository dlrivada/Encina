```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.5886 ns |  0.0599 ns |  0.0800 ns |    43.6062 ns |  0.976 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1460 ns |  0.0057 ns |  0.0079 ns |    12.1462 ns |  0.272 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1356 ns |  0.1010 ns |  0.1382 ns |     0.1347 ns |  0.003 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,654.7751 ns | 15.6347 ns | 21.9176 ns | 3,667.9186 ns | 81.851 |    0.54 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    44.6519 ns |  0.0931 ns |  0.1335 ns |    44.6821 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,954.3277 ns | 22.0240 ns | 30.8746 ns | 1,972.3633 ns | 43.768 |    0.69 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,763.4189 ns | 39.7126 ns | 58.2102 ns | 3,754.8817 ns | 84.284 |    1.31 | 0.1335 |    2248 B |          NA |
