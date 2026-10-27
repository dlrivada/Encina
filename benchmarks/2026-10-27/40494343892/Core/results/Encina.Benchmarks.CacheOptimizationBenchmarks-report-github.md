```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.70GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    37.8456 ns |   0.9806 ns |  0.0537 ns |   1.00 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4288 ns |   0.0848 ns |  0.0046 ns |   0.33 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.4849 ns |   0.6534 ns |  0.0358 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,094.0034 ns | 276.5275 ns | 15.1574 ns | 107.98 |    0.49 | 0.1068 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    37.9141 ns |   2.5942 ns |  0.1422 ns |   1.00 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,225.1450 ns |  99.3436 ns |  5.4454 ns |  85.07 |    0.30 | 0.0877 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,244.7100 ns | 406.6621 ns | 22.2905 ns | 111.96 |    0.63 | 0.1068 |    2832 B |          NA |
