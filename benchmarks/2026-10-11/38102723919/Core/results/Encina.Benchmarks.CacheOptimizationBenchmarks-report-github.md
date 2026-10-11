```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    37.2498 ns |  0.6894 ns |  1.0106 ns |    38.1378 ns |   0.96 |    0.04 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1756 ns |  0.0511 ns |  0.0750 ns |    12.2239 ns |   0.31 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.6575 ns |  0.0229 ns |  0.0342 ns |     0.6637 ns |   0.02 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,923.9813 ns | 13.9805 ns | 20.9254 ns | 3,923.2361 ns | 101.27 |    3.14 | 0.1144 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    38.7851 ns |  0.8201 ns |  1.2021 ns |    39.8289 ns |   1.00 |    0.04 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,227.8498 ns | 41.4228 ns | 61.9997 ns | 3,242.1684 ns |  83.30 |    2.99 | 0.0877 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,199.0432 ns | 13.2310 ns | 19.8035 ns | 4,203.7864 ns | 108.37 |    3.35 | 0.1144 |    2936 B |          NA |
