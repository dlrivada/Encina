```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    30.5224 ns |    19.8476 ns |  1.0879 ns |  0.91 |    0.03 |      - |         - |          NA |
| TypeCheck_Cached               |    10.1730 ns |     3.4080 ns |  0.1868 ns |  0.30 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.4972 ns |     0.5451 ns |  0.0299 ns |  0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,661.2992 ns | 1,565.5695 ns | 85.8141 ns | 79.35 |    2.61 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.5495 ns |    12.1695 ns |  0.6671 ns |  1.00 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,345.8142 ns |   284.1171 ns | 15.5734 ns | 69.94 |    1.28 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,867.5772 ns |   233.1006 ns | 12.7770 ns | 85.50 |    1.52 | 0.0305 |    2832 B |          NA |
