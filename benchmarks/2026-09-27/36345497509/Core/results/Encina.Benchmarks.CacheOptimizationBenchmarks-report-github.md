```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    24.1250 ns |     2.3291 ns |   0.1277 ns |  1.00 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     7.4598 ns |     3.2958 ns |   0.1807 ns |  0.31 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2454 ns |     0.2201 ns |   0.0121 ns |  0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,242.3899 ns |   346.7312 ns |  19.0055 ns | 93.21 |    1.44 | 0.1564 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    24.0620 ns |     6.9482 ns |   0.3809 ns |  1.00 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,986.0263 ns | 1,981.7348 ns | 108.6256 ns | 82.55 |    4.07 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 2,308.6171 ns |   337.0871 ns |  18.4769 ns | 95.96 |    1.47 | 0.1526 |    2600 B |          NA |
