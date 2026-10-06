```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.4103 ns |   0.3644 ns |  0.0200 ns |   0.993 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5155 ns |   3.9096 ns |  0.2143 ns |   0.300 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3025 ns |   0.1176 ns |  0.0064 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,306.7027 ns | 307.8563 ns | 16.8746 ns | 103.299 |    0.46 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    41.6920 ns |   2.5234 ns |  0.1383 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,514.7774 ns | 174.8760 ns |  9.5855 ns |  84.304 |    0.31 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,405.0102 ns |  82.8581 ns |  4.5417 ns | 105.657 |    0.32 | 0.1755 |    2936 B |          NA |
