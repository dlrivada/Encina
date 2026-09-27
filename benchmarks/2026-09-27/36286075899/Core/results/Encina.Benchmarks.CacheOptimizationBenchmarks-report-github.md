```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.6470 ns |  0.7594 ns |  1.1131 ns |    43.4401 ns |  0.914 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5064 ns |  0.0132 ns |  0.0190 ns |    12.5034 ns |  0.268 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3509 ns |  0.0011 ns |  0.0015 ns |     0.3505 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,956.2847 ns | 57.2678 ns | 83.9424 ns | 4,017.6026 ns | 84.781 |    1.77 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.6650 ns |  0.0277 ns |  0.0397 ns |    46.6652 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,214.8996 ns | 20.9466 ns | 31.3519 ns | 3,217.4260 ns | 68.893 |    0.66 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,110.5213 ns | 34.0043 ns | 49.8431 ns | 4,089.3684 ns | 88.086 |    1.05 | 0.1526 |    2600 B |          NA |
