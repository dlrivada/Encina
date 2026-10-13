```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    24.2368 ns |     7.9746 ns |  0.4371 ns |   1.000 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     7.3277 ns |     0.5201 ns |  0.0285 ns |   0.302 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2166 ns |     0.4645 ns |  0.0255 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,529.6917 ns |   638.7971 ns | 35.0146 ns | 104.335 |    1.59 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    24.2477 ns |     4.7419 ns |  0.2599 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,106.2812 ns | 1,360.2426 ns | 74.5595 ns |  86.872 |    2.78 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,591.5263 ns |   346.0573 ns | 18.9686 ns | 106.886 |    1.21 | 0.1755 |    2936 B |          NA |
