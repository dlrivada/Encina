```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    40.8376 ns |   0.8724 ns |  0.0478 ns |  0.841 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4480 ns |   0.1651 ns |  0.0090 ns |  0.256 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2980 ns |   0.0803 ns |  0.0044 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,527.6921 ns | 494.7975 ns | 27.1215 ns | 93.224 |    0.58 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    48.5683 ns |   3.5414 ns |  0.1941 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,735.0903 ns |  95.6982 ns |  5.2455 ns | 76.905 |    0.28 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,809.7107 ns |  70.9133 ns |  3.8870 ns | 99.031 |    0.35 | 0.1755 |    2936 B |          NA |
