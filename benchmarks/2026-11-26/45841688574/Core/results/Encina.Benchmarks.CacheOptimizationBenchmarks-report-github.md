```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Median        | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    35.4930 ns |   9.8985 ns |  0.5426 ns |    35.6651 ns |   1.081 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.7055 ns |   4.1905 ns |  0.2297 ns |    10.8013 ns |   0.326 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0209 ns |   0.2305 ns |  0.0126 ns |     0.0152 ns |   0.001 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,619.3387 ns | 379.8145 ns | 20.8189 ns | 3,628.5014 ns | 110.216 |    0.64 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.8388 ns |   2.0690 ns |  0.1134 ns |    32.8029 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,920.9408 ns | 115.1953 ns |  6.3142 ns | 2,918.5529 ns |  88.949 |    0.31 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,846.7669 ns | 770.0742 ns | 42.2104 ns | 3,822.8403 ns | 117.142 |    1.17 | 0.1678 |    2832 B |          NA |
