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
| Cache_TryGetValue_ThenGetOrAdd |    33.7127 ns |   1.1771 ns |  0.0645 ns |   0.992 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.1504 ns |   3.5480 ns |  0.1945 ns |   0.299 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2435 ns |   0.0045 ns |  0.0002 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,378.9225 ns | 465.6623 ns | 25.5245 ns |  99.471 |    0.76 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    33.9694 ns |   2.7694 ns |  0.1518 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,797.4568 ns | 230.1402 ns | 12.6148 ns |  82.353 |    0.45 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,512.9863 ns | 205.3915 ns | 11.2582 ns | 103.418 |    0.49 | 0.1755 |    2936 B |          NA |
