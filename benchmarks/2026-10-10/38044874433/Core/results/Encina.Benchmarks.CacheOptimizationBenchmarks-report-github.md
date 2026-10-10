```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.2325 ns |   9.8024 ns |  0.5373 ns |   1.038 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5079 ns |   0.1368 ns |  0.0075 ns |   0.300 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3556 ns |   0.1348 ns |  0.0074 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,234.3737 ns | 173.9252 ns |  9.5334 ns | 101.699 |    0.21 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    41.6362 ns |   0.5541 ns |  0.0304 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,595.7112 ns | 282.4805 ns | 15.4837 ns |  86.360 |    0.33 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,408.0927 ns | 190.6727 ns | 10.4514 ns | 105.872 |    0.23 | 0.1755 |    2936 B |          NA |
