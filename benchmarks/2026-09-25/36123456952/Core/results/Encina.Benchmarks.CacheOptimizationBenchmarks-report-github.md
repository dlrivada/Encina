```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    44.0024 ns |   6.2176 ns |  0.3408 ns |  0.944 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5105 ns |   0.1693 ns |  0.0093 ns |  0.269 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3583 ns |   0.1813 ns |  0.0099 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,872.1983 ns | 572.9406 ns | 31.4048 ns | 83.108 |    0.58 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.5924 ns |   0.3429 ns |  0.0188 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,210.1996 ns | 201.8326 ns | 11.0631 ns | 68.900 |    0.21 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,111.4469 ns | 372.6585 ns | 20.4267 ns | 88.243 |    0.38 | 0.1526 |    2600 B |          NA |
