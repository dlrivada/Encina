```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.9778 ns |     4.5303 ns |   0.2483 ns |  0.943 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.2911 ns |     1.8713 ns |   0.1026 ns |  0.298 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3446 ns |     0.9586 ns |   0.0525 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,957.5650 ns |   307.2743 ns |  16.8427 ns | 66.407 |    0.77 | 0.0305 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    44.5414 ns |     9.8111 ns |   0.5378 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,841.0011 ns | 2,200.5462 ns | 120.6194 ns | 63.790 |    2.44 | 0.0191 |    1896 B |          NA |
| Send_Query_CacheHit            | 3,195.9195 ns |   560.2150 ns |  30.7073 ns | 71.759 |    0.96 | 0.0305 |    2600 B |          NA |
