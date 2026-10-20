```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    34.0689 ns |   1.8676 ns |  0.1024 ns |   1.01 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.4091 ns |   1.4410 ns |  0.0790 ns |   0.31 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3975 ns |   2.6203 ns |  0.1436 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,538.4086 ns | 388.7004 ns | 21.3060 ns | 105.30 |    0.62 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.6032 ns |   1.8792 ns |  0.1030 ns |   1.00 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,861.1968 ns | 179.2764 ns |  9.8267 ns |  85.15 |    0.34 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,772.4240 ns | 131.3642 ns |  7.2005 ns | 112.26 |    0.35 | 0.1678 |    2832 B |          NA |
