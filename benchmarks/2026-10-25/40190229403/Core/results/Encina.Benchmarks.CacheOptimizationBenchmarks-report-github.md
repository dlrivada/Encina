```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    40.8556 ns |   1.7989 ns |  0.0986 ns |  0.892 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.5663 ns |   0.8213 ns |  0.0450 ns |  0.296 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0448 ns |   0.8183 ns |  0.0449 ns |  0.001 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,383.7966 ns | 288.7382 ns | 15.8267 ns | 73.839 |    0.43 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    45.8275 ns |   4.1280 ns |  0.2263 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,792.8083 ns | 659.0363 ns | 36.1240 ns | 60.943 |    0.73 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,549.7566 ns | 219.2965 ns | 12.0204 ns | 77.460 |    0.40 | 0.0305 |    2832 B |          NA |
