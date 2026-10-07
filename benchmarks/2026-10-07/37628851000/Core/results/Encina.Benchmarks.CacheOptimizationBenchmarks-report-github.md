```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    24.2087 ns |    28.0389 ns |   1.5369 ns |   1.007 |    0.06 |      - |         - |          NA |
| TypeCheck_Cached               |     7.1945 ns |     2.4879 ns |   0.1364 ns |   0.299 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2372 ns |     0.4294 ns |   0.0235 ns |   0.010 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,390.3071 ns |   315.7413 ns |  17.3068 ns |  99.469 |    0.87 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    24.0314 ns |     3.0758 ns |   0.1686 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,165.1869 ns | 2,208.4417 ns | 121.0521 ns |  90.101 |    4.40 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,490.3910 ns |   262.6699 ns |  14.3978 ns | 103.634 |    0.82 | 0.1755 |    2936 B |          NA |
