```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.8227 ns |     0.5116 ns |  0.0280 ns |   1.023 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    13.1066 ns |    10.1829 ns |  0.5582 ns |   0.313 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3782 ns |     0.7695 ns |  0.0422 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,347.4518 ns | 1,150.6637 ns | 63.0718 ns | 103.830 |    1.30 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    41.8711 ns |     0.2896 ns |  0.0159 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,519.8708 ns |   274.5659 ns | 15.0499 ns |  84.065 |    0.31 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,309.1563 ns |   397.5222 ns | 21.7895 ns | 102.915 |    0.45 | 0.1755 |    2936 B |          NA |
