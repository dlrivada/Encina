```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    40.8508 ns |   2.4633 ns |  0.1350 ns |   0.962 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5302 ns |   0.4131 ns |  0.0226 ns |   0.295 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3824 ns |   0.0441 ns |  0.0024 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,296.8813 ns | 445.8015 ns | 24.4359 ns | 101.231 |    0.51 | 0.1755 |    2952 B |          NA |
| Cache_GetOrAdd_Direct          |    42.4463 ns |   0.8612 ns |  0.0472 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,532.3116 ns | 232.6474 ns | 12.7522 ns |  83.218 |    0.27 | 0.1297 |    2224 B |          NA |
| Send_Query_CacheHit            | 4,445.1463 ns |  69.3918 ns |  3.8036 ns | 104.724 |    0.13 | 0.1678 |    2928 B |          NA |
