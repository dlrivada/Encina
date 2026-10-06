```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.9871 ns |   7.9839 ns |  0.4376 ns |   0.997 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.8416 ns |   1.9181 ns |  0.1051 ns |   0.291 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3943 ns |   0.0171 ns |  0.0009 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,390.9736 ns |  46.0324 ns |  2.5232 ns |  99.514 |    0.09 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    44.1244 ns |   0.6984 ns |  0.0383 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,649.1873 ns | 200.3155 ns | 10.9800 ns |  82.702 |    0.22 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,493.4674 ns | 241.1489 ns | 13.2182 ns | 101.836 |    0.27 | 0.1755 |    2936 B |          NA |
