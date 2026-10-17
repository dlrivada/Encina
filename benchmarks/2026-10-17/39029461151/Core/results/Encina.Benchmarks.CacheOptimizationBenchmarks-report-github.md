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
| Cache_TryGetValue_ThenGetOrAdd |    41.1402 ns |   1.3930 ns |  0.0764 ns |   0.956 |    0.04 |      - |         - |          NA |
| TypeCheck_Cached               |    13.1211 ns |   0.2558 ns |  0.0140 ns |   0.305 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3496 ns |   0.0082 ns |  0.0004 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,506.6911 ns | 357.7794 ns | 19.6111 ns | 104.778 |    4.11 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    43.0714 ns |  36.0949 ns |  1.9785 ns |   1.001 |    0.06 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,535.9114 ns | 172.6633 ns |  9.4643 ns |  82.208 |    3.22 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,608.6979 ns | 218.1283 ns | 11.9563 ns | 107.149 |    4.19 | 0.1678 |    2832 B |          NA |
