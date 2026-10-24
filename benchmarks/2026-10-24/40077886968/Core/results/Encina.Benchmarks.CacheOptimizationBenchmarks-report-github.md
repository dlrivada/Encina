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
| Cache_TryGetValue_ThenGetOrAdd |    42.2041 ns |   9.3837 ns |  0.5144 ns |   1.017 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4710 ns |   0.2025 ns |  0.0111 ns |   0.300 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2719 ns |   0.0152 ns |  0.0008 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,583.7796 ns | 102.6379 ns |  5.6259 ns | 110.411 |    0.13 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.5158 ns |   0.3818 ns |  0.0209 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,682.8539 ns |  81.4321 ns |  4.4636 ns |  88.710 |    0.10 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,782.3912 ns | 260.3188 ns | 14.2690 ns | 115.195 |    0.30 | 0.1678 |    2832 B |          NA |
