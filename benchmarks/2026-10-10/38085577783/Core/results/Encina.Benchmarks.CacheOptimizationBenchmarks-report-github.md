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
| Cache_TryGetValue_ThenGetOrAdd |    42.3585 ns |   0.3039 ns |  0.0167 ns |   1.009 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5244 ns |   0.8536 ns |  0.0468 ns |   0.298 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3535 ns |   0.0153 ns |  0.0008 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,166.8316 ns | 533.1487 ns | 29.2237 ns |  99.239 |    0.67 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    41.9880 ns |   2.5536 ns |  0.1400 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,468.4221 ns | 108.0231 ns |  5.9211 ns |  82.606 |    0.27 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,382.0933 ns | 211.8258 ns | 11.6109 ns | 104.366 |    0.39 | 0.1755 |    2936 B |          NA |
