```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    24.1075 ns |     6.1316 ns |   0.3361 ns |   0.975 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     7.4430 ns |     0.7421 ns |   0.0407 ns |   0.301 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2453 ns |     0.2994 ns |   0.0164 ns |   0.010 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,520.0232 ns |   331.1680 ns |  18.1524 ns | 101.940 |    2.15 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    24.7296 ns |    10.6575 ns |   0.5842 ns |   1.000 |    0.03 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,080.1686 ns |   367.3602 ns |  20.1363 ns |  84.147 |    1.84 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,702.5243 ns | 2,143.5023 ns | 117.4926 ns | 109.323 |    4.67 | 0.1755 |    2936 B |          NA |
