```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.6104 ns |  0.0567 ns |  0.0813 ns |  0.951 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1582 ns |  0.0187 ns |  0.0256 ns |  0.265 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2725 ns |  0.0016 ns |  0.0023 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,669.6301 ns | 19.6224 ns | 27.5079 ns | 80.008 |    0.63 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    45.8659 ns |  0.0872 ns |  0.1278 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,984.8382 ns | 19.6008 ns | 28.7306 ns | 43.275 |    0.63 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,727.9695 ns | 25.2384 ns | 36.9941 ns | 81.280 |    0.82 | 0.1335 |    2248 B |          NA |
