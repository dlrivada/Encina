```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.7520 ns |     1.9477 ns |  0.1068 ns |  0.934 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5119 ns |     0.1329 ns |  0.0073 ns |  0.267 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3501 ns |     0.0326 ns |  0.0018 ns |  0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,167.5397 ns | 1,478.4627 ns | 81.0395 ns | 88.942 |    1.50 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.8567 ns |     0.8467 ns |  0.0464 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,368.7560 ns |    84.3337 ns |  4.6226 ns | 71.895 |    0.11 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,149.0359 ns |   156.2873 ns |  8.5666 ns | 88.547 |    0.18 | 0.1526 |    2600 B |          NA |
