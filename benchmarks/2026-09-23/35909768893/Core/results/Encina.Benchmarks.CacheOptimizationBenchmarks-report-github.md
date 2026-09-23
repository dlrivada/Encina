```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.6834 ns |   1.3356 ns |  0.0732 ns |  0.946 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.2482 ns |   1.2487 ns |  0.0684 ns |  0.278 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2885 ns |   0.2876 ns |  0.0158 ns |  0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,968.1006 ns | 161.1102 ns |  8.8310 ns | 90.061 |    0.28 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    44.0605 ns |   2.2281 ns |  0.1221 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,246.7546 ns | 171.8905 ns |  9.4219 ns | 73.689 |    0.26 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,143.2300 ns | 654.2072 ns | 35.8593 ns | 94.036 |    0.74 | 0.1526 |    2600 B |          NA |
