```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.76GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    28.9190 ns |   1.9552 ns |  0.1072 ns |  0.94 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |     9.2471 ns |   0.1840 ns |  0.0101 ns |  0.30 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3106 ns |   0.1253 ns |  0.0069 ns |  0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,116.6588 ns |  76.5110 ns |  4.1938 ns | 68.62 |    0.58 | 0.0305 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    30.8467 ns |   5.3861 ns |  0.2952 ns |  1.00 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,917.3286 ns | 373.7280 ns | 20.4853 ns | 62.16 |    0.77 | 0.0210 |    1896 B |          NA |
| Send_Query_CacheHit            | 2,297.4451 ns | 193.8017 ns | 10.6229 ns | 74.48 |    0.68 | 0.0305 |    2600 B |          NA |
