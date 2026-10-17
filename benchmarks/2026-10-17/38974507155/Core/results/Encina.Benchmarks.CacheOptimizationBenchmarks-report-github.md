```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.7661 ns |   5.1021 ns |  0.2797 ns |  0.988 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.8965 ns |   0.8641 ns |  0.0474 ns |  0.314 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3802 ns |   0.9931 ns |  0.0544 ns |  0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,375.1770 ns | 721.1564 ns | 39.5290 ns | 76.189 |    0.78 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    44.2999 ns |   1.6885 ns |  0.0926 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,796.9591 ns | 269.0607 ns | 14.7481 ns | 63.137 |    0.31 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,588.2857 ns | 673.7807 ns | 36.9322 ns | 81.000 |    0.74 | 0.0305 |    2832 B |          NA |
