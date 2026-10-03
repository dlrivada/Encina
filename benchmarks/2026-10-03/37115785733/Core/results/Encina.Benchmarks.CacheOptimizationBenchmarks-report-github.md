```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    29.0106 ns |   6.8554 ns |  0.3758 ns |  0.969 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     9.2174 ns |   0.1404 ns |  0.0077 ns |  0.308 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2967 ns |   0.0247 ns |  0.0014 ns |  0.010 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,257.9403 ns | 419.3981 ns | 22.9886 ns | 75.396 |    0.67 | 0.0305 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    29.9480 ns |   0.8861 ns |  0.0486 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,027.3380 ns | 403.0117 ns | 22.0904 ns | 67.695 |    0.65 | 0.0210 |    1896 B |          NA |
| Send_Query_CacheHit            | 2,338.1896 ns | 141.1163 ns |  7.7351 ns | 78.075 |    0.25 | 0.0305 |    2600 B |          NA |
