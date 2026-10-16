```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.4072 ns |   2.6066 ns |  0.1429 ns |   1.014 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1552 ns |   0.3952 ns |  0.0217 ns |   0.284 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2716 ns |   0.0156 ns |  0.0009 ns |   0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,535.8764 ns | 237.8667 ns | 13.0383 ns | 105.925 |    0.29 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.8215 ns |   1.1193 ns |  0.0614 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,495.2137 ns |  33.4700 ns |  1.8346 ns |  81.623 |    0.11 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,528.6748 ns | 262.3473 ns | 14.3801 ns | 105.757 |    0.32 | 0.1678 |    2832 B |          NA |
