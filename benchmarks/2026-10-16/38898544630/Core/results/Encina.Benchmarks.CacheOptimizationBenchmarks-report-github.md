```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    40.9526 ns |   4.4907 ns |  0.2462 ns |   0.974 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4591 ns |   0.2559 ns |  0.0140 ns |   0.296 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2991 ns |   0.0407 ns |  0.0022 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,521.2897 ns | 212.3125 ns | 11.6376 ns | 107.578 |    0.44 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    42.0283 ns |   3.0699 ns |  0.1683 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,652.9761 ns | 181.7231 ns |  9.9609 ns |  86.918 |    0.36 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,709.4738 ns |  80.1174 ns |  4.3915 ns | 112.056 |    0.40 | 0.1755 |    2936 B |          NA |
