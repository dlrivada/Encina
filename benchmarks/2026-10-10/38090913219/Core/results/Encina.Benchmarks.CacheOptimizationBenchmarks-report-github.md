```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.6378 ns |   5.4568 ns |  0.2991 ns |   0.985 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.2358 ns |   2.4386 ns |  0.1337 ns |   0.300 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2879 ns |   0.0097 ns |  0.0005 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,311.4964 ns | 166.4227 ns |  9.1222 ns |  97.005 |    0.70 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    34.1386 ns |   4.8863 ns |  0.2678 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,730.0690 ns |  75.0987 ns |  4.1164 ns |  79.973 |    0.55 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,477.3384 ns | 217.4933 ns | 11.9215 ns | 101.864 |    0.75 | 0.1755 |    2936 B |          NA |
