```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    37.7467 ns |   1.1149 ns |  0.0611 ns |  0.960 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.3019 ns |   0.9113 ns |  0.0500 ns |  0.262 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3612 ns |   2.2700 ns |  0.1244 ns |  0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,463.5629 ns | 584.9766 ns | 32.0645 ns | 62.647 |    0.72 | 0.0305 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    39.3249 ns |   1.5091 ns |  0.0827 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,173.5277 ns | 385.0949 ns | 21.1084 ns | 55.271 |    0.48 | 0.0191 |    1896 B |          NA |
| Send_Query_CacheHit            | 2,635.6360 ns |  22.9806 ns |  1.2596 ns | 67.022 |    0.13 | 0.0305 |    2600 B |          NA |
