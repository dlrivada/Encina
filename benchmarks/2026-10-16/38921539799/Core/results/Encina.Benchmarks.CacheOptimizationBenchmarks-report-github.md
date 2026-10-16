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
| Cache_TryGetValue_ThenGetOrAdd |    41.7560 ns |   3.1093 ns |  0.1704 ns |   1.005 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4800 ns |   0.2235 ns |  0.0123 ns |   0.300 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2758 ns |   0.0731 ns |  0.0040 ns |   0.007 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,412.7835 ns | 430.3020 ns | 23.5863 ns | 106.220 |    0.49 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.5437 ns |   0.2315 ns |  0.0127 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,576.1015 ns | 670.4131 ns | 36.7476 ns |  86.080 |    0.77 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,649.9941 ns | 283.3900 ns | 15.5336 ns | 111.930 |    0.33 | 0.1678 |    2832 B |          NA |
