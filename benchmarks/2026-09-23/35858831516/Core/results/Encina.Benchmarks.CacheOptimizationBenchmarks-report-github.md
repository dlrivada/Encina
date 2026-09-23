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
| Cache_TryGetValue_ThenGetOrAdd |    40.4525 ns |   0.2578 ns |  0.0141 ns |  0.908 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    11.7982 ns |   0.0353 ns |  0.0019 ns |  0.265 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2808 ns |   0.1863 ns |  0.0102 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,646.8730 ns | 140.6254 ns |  7.7082 ns | 81.836 |    0.32 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    44.5634 ns |   3.2081 ns |  0.1758 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,990.8973 ns |  97.0152 ns |  5.3177 ns | 44.676 |    0.18 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,770.6914 ns | 370.5991 ns | 20.3138 ns | 84.615 |    0.49 | 0.1335 |    2248 B |          NA |
