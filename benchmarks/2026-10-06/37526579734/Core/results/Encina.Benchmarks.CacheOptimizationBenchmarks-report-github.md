```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    24.4922 ns |   8.0261 ns |  0.4399 ns |   0.976 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     7.3370 ns |   1.0062 ns |  0.0552 ns |   0.292 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2036 ns |   1.4517 ns |  0.0796 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,554.5954 ns | 369.7718 ns | 20.2684 ns | 101.770 |    2.16 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    25.1106 ns |  10.4983 ns |  0.5754 ns |   1.000 |    0.03 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,121.4678 ns |  45.2484 ns |  2.4802 ns |  84.515 |    1.70 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,643.9792 ns | 525.0724 ns | 28.7810 ns | 105.331 |    2.33 | 0.1755 |    2936 B |          NA |
