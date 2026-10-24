```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    30.8029 ns |    21.3726 ns |   1.1715 ns |  1.00 |    0.05 |      - |         - |          NA |
| TypeCheck_Cached               |     9.9308 ns |     2.6091 ns |   0.1430 ns |  0.32 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.5109 ns |     0.3406 ns |   0.0187 ns |  0.02 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,503.0483 ns | 1,211.2864 ns |  66.3947 ns | 81.24 |    3.39 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    30.8453 ns |    23.0438 ns |   1.2631 ns |  1.00 |    0.05 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,071.3215 ns |   544.7969 ns |  29.8622 ns | 67.23 |    2.49 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,676.0823 ns | 2,703.8352 ns | 148.2063 ns | 86.85 |    5.15 | 0.0305 |    2832 B |          NA |
