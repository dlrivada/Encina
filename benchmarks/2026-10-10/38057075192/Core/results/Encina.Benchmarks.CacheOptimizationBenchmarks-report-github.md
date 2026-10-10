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
| Cache_TryGetValue_ThenGetOrAdd |    23.0815 ns |     4.6631 ns |   0.2556 ns |   0.964 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     7.2618 ns |     2.6422 ns |   0.1448 ns |   0.303 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2325 ns |     1.0487 ns |   0.0575 ns |   0.010 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,515.1002 ns | 3,871.0388 ns | 212.1847 ns | 104.992 |    7.70 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    23.9559 ns |     3.1476 ns |   0.1725 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,996.9219 ns |   287.7228 ns |  15.7711 ns |  83.361 |    0.77 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,511.7199 ns |   140.3373 ns |   7.6924 ns | 104.851 |    0.71 | 0.1755 |    2936 B |          NA |
