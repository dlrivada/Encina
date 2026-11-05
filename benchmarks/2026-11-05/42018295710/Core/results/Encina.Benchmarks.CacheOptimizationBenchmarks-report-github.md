```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    23.1045 ns |     6.0004 ns |  0.3289 ns |   0.952 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     7.1857 ns |     3.5433 ns |  0.1942 ns |   0.296 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1960 ns |     1.0150 ns |  0.0556 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,650.5677 ns |   647.5591 ns | 35.4949 ns | 109.270 |    1.38 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    24.2576 ns |     2.6055 ns |  0.1428 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,149.9052 ns | 1,093.9666 ns | 59.9640 ns |  88.630 |    2.19 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,772.6278 ns |   241.6403 ns | 13.2451 ns | 114.302 |    0.75 | 0.1678 |    2832 B |          NA |
