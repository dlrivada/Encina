```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.4933 ns |  1.8304 ns | 0.1003 ns |  0.948 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1679 ns |  0.5881 ns | 0.0322 ns |  0.265 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2797 ns |  0.2873 ns | 0.0157 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,648.4044 ns | 69.3939 ns | 3.8037 ns | 79.536 |    0.10 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    45.8710 ns |  0.8796 ns | 0.0482 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,978.7265 ns | 97.4575 ns | 5.3420 ns | 43.137 |    0.11 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,820.2427 ns | 66.2061 ns | 3.6290 ns | 83.282 |    0.10 | 0.1335 |    2248 B |          NA |
