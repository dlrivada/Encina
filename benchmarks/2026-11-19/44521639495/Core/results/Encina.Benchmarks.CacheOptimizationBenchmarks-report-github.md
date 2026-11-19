```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    24.8874 ns |     3.8232 ns |   0.2096 ns |   0.950 |    0.03 |      - |         - |          NA |
| TypeCheck_Cached               |     8.1648 ns |     0.7342 ns |   0.0402 ns |   0.312 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2113 ns |     0.4616 ns |   0.0253 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,653.4880 ns |   479.8766 ns |  26.3037 ns | 101.308 |    3.10 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    26.2122 ns |    16.1212 ns |   0.8837 ns |   1.001 |    0.04 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,265.3914 ns | 3,236.2677 ns | 177.3908 ns |  86.491 |    6.39 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,768.7944 ns |   284.0280 ns |  15.5685 ns | 105.711 |    3.14 | 0.1678 |    2832 B |          NA |
