```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    40.6310 ns |   0.5529 ns | 0.0303 ns |  0.919 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1711 ns |   1.3037 ns | 0.0715 ns |  0.275 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2705 ns |   0.0121 ns | 0.0007 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,542.0254 ns |  30.2511 ns | 1.6582 ns | 80.141 |    0.09 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    44.1975 ns |   0.9769 ns | 0.0535 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,956.1926 ns | 129.9121 ns | 7.1209 ns | 44.260 |    0.15 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,743.9163 ns | 131.4686 ns | 7.2062 ns | 84.709 |    0.17 | 0.1335 |    2248 B |          NA |
