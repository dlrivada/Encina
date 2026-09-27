```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.8725 ns |   8.6505 ns |  0.4742 ns |  0.939 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5807 ns |   2.0923 ns |  0.1147 ns |  0.269 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3648 ns |   0.2568 ns |  0.0141 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,045.4050 ns | 327.3543 ns | 17.9434 ns | 86.576 |    0.33 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    46.7266 ns |   0.4292 ns |  0.0235 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,333.8028 ns | 362.1140 ns | 19.8487 ns | 71.347 |    0.37 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,195.5630 ns | 494.5554 ns | 27.1083 ns | 89.790 |    0.50 | 0.1526 |    2600 B |          NA |
