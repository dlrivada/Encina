```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    22.8432 ns |     4.7268 ns |   0.2591 ns |  0.97 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     6.8649 ns |     1.0472 ns |   0.0574 ns |  0.29 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2529 ns |     0.1869 ns |   0.0102 ns |  0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,188.0954 ns |    56.7646 ns |   3.1115 ns | 93.22 |    0.56 | 0.1564 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    23.4719 ns |     2.9055 ns |   0.1593 ns |  1.00 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,844.4370 ns |   547.8664 ns |  30.0304 ns | 78.58 |    1.20 | 0.1125 |    1896 B |          NA |
| Send_Query_CacheHit            | 2,319.5530 ns | 2,146.2727 ns | 117.6445 ns | 98.83 |    4.38 | 0.1526 |    2600 B |          NA |
