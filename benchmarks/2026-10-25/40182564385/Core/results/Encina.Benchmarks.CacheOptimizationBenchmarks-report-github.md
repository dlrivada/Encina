```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.63GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    35.6441 ns |   0.0934 ns |  0.0051 ns |   0.95 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    11.3548 ns |   2.3165 ns |  0.1270 ns |   0.30 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.4587 ns |   1.5149 ns |  0.0830 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,987.9971 ns | 251.6346 ns | 13.7929 ns | 106.12 |    0.32 | 0.1068 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    37.5802 ns |   0.4661 ns |  0.0256 ns |   1.00 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,150.6253 ns | 231.3684 ns | 12.6821 ns |  83.84 |    0.30 | 0.0877 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,141.2066 ns | 204.8588 ns | 11.2290 ns | 110.20 |    0.27 | 0.1068 |    2832 B |          NA |
