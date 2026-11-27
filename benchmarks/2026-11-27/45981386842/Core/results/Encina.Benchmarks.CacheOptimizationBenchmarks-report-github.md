```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.8734 ns |     2.9085 ns |   0.1594 ns |   1.023 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    10.7062 ns |     3.0609 ns |   0.1678 ns |   0.323 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2833 ns |     0.0284 ns |   0.0016 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,542.2204 ns |    63.8626 ns |   3.5005 ns | 107.010 |    2.52 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.1183 ns |    16.6396 ns |   0.9121 ns |   1.001 |    0.03 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,843.0482 ns |   160.7803 ns |   8.8129 ns |  85.888 |    2.04 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,758.8575 ns | 2,066.0655 ns | 113.2480 ns | 113.555 |    3.99 | 0.1678 |    2832 B |          NA |
