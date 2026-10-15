```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.2146 ns |     2.5214 ns |   0.1382 ns |   0.987 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.8373 ns |     3.3846 ns |   0.1855 ns |   0.293 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3945 ns |     0.0339 ns |   0.0019 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,495.5254 ns | 2,405.4295 ns | 131.8497 ns | 102.635 |    2.64 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    43.8016 ns |     3.6818 ns |   0.2018 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,737.5478 ns |   263.0862 ns |  14.4206 ns |  85.330 |    0.44 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,682.7848 ns | 2,639.4210 ns | 144.6756 ns | 106.910 |    2.89 | 0.1755 |    2936 B |          NA |
