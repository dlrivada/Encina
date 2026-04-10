```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    44.1510 ns |  15.3486 ns |  0.8413 ns |  1.002 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1484 ns |   0.0902 ns |  0.0049 ns |  0.276 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2838 ns |   0.3268 ns |  0.0179 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,600.2554 ns | 295.8699 ns | 16.2176 ns | 81.686 |    0.37 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    44.0747 ns |   2.0976 ns |  0.1150 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,004.9567 ns | 123.5629 ns |  6.7729 ns | 45.490 |    0.17 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,710.2592 ns |  58.5500 ns |  3.2093 ns | 84.182 |    0.20 | 0.1335 |    2248 B |          NA |
