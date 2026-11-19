```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.46GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    38.1564 ns |   2.3804 ns |  0.1305 ns |   0.96 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.8409 ns |   1.9394 ns |  0.1063 ns |   0.32 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.6338 ns |   0.3586 ns |  0.0197 ns |   0.02 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,955.8441 ns | 300.0949 ns | 16.4492 ns |  99.92 |    0.58 | 0.1068 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    39.5893 ns |   3.8428 ns |  0.2106 ns |   1.00 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,153.8150 ns | 202.6207 ns | 11.1063 ns |  79.66 |    0.44 | 0.0877 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,237.1650 ns | 128.8801 ns |  7.0644 ns | 107.03 |    0.52 | 0.1068 |    2832 B |          NA |
