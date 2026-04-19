```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    39.7531 ns |  0.0341 ns |  0.0467 ns |    39.7410 ns |  0.941 |    0.03 |      - |         - |          NA |
| TypeCheck_Cached               |    11.8201 ns |  0.0142 ns |  0.0198 ns |    11.8162 ns |  0.280 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2724 ns |  0.0010 ns |  0.0013 ns |     0.2723 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,562.1030 ns |  9.7178 ns | 14.5451 ns | 3,565.9137 ns | 84.344 |    2.61 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    42.2730 ns |  0.9217 ns |  1.3218 ns |    42.2953 ns |  1.001 |    0.04 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,962.0010 ns | 19.0109 ns | 26.6506 ns | 1,978.0782 ns | 46.456 |    1.56 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,717.5912 ns | 27.3611 ns | 40.1056 ns | 3,707.7053 ns | 88.025 |    2.86 | 0.1335 |    2248 B |          NA |
