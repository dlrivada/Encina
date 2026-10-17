```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    38.3216 ns |   5.6047 ns |  0.3072 ns |   0.97 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    11.9610 ns |   0.1179 ns |  0.0065 ns |   0.30 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.4746 ns |   0.4222 ns |  0.0231 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,104.7576 ns | 215.8424 ns | 11.8311 ns | 103.95 |    0.26 | 0.1068 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    39.4882 ns |   0.2514 ns |  0.0138 ns |   1.00 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,290.7456 ns | 164.5988 ns |  9.0222 ns |  83.33 |    0.20 | 0.0877 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,330.8985 ns | 178.8694 ns |  9.8044 ns | 109.68 |    0.22 | 0.1068 |    2832 B |          NA |
