```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    23.6090 ns |    10.9744 ns |   0.6015 ns |   0.977 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     7.2683 ns |     3.6664 ns |   0.2010 ns |   0.301 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1923 ns |     0.1263 ns |   0.0069 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,646.2272 ns |   562.3044 ns |  30.8218 ns | 109.501 |    1.49 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    24.1680 ns |     4.6093 ns |   0.2527 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,059.6544 ns |   110.8477 ns |   6.0759 ns |  85.229 |    0.81 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,817.1032 ns | 2,274.2992 ns | 124.6620 ns | 116.572 |    4.59 | 0.1678 |    2832 B |          NA |
