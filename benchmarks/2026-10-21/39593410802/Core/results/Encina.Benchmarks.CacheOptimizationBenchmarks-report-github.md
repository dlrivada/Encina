```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    36.1897 ns |   0.3881 ns |  0.0213 ns |   0.96 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    11.3044 ns |   0.0855 ns |  0.0047 ns |   0.30 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.7133 ns |   0.3835 ns |  0.0210 ns |   0.02 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,084.0627 ns | 183.9068 ns | 10.0806 ns | 108.87 |    0.24 | 0.1068 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    37.5141 ns |   0.5339 ns |  0.0293 ns |   1.00 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,235.8335 ns | 408.6372 ns | 22.3988 ns |  86.26 |    0.52 | 0.0877 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,371.4588 ns |  88.9569 ns |  4.8760 ns | 116.53 |    0.14 | 0.1068 |    2832 B |          NA |
