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
| Cache_TryGetValue_ThenGetOrAdd |    41.3687 ns |   2.8115 ns |  0.1541 ns |  0.919 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4973 ns |   0.1017 ns |  0.0056 ns |  0.278 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3545 ns |   0.1216 ns |  0.0067 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,132.9742 ns | 162.6171 ns |  8.9136 ns | 91.834 |    0.17 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    45.0046 ns |   0.1253 ns |  0.0069 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,533.9565 ns |  90.1065 ns |  4.9390 ns | 78.524 |    0.10 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,425.7878 ns | 272.2208 ns | 14.9213 ns | 98.341 |    0.29 | 0.1755 |    2936 B |          NA |
