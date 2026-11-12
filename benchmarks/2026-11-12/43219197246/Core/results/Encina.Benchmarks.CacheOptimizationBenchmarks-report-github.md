```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.3427 ns |   1.8047 ns |  0.0989 ns |   0.985 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.6364 ns |   2.7939 ns |  0.1531 ns |   0.314 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0185 ns |   0.0096 ns |  0.0005 ns |   0.001 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,683.5527 ns | 177.9305 ns |  9.7530 ns | 108.822 |    1.48 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.8547 ns |   9.5454 ns |  0.5232 ns |   1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,994.7772 ns | 241.8332 ns | 13.2557 ns |  88.474 |    1.23 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,765.8534 ns | 331.4148 ns | 18.1660 ns | 111.254 |    1.56 | 0.1678 |    2832 B |          NA |
