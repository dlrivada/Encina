```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev      | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    33.1140 ns |     5.6562 ns |   0.3100 ns |   0.997 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    11.7723 ns |     1.7014 ns |   0.0933 ns |   0.354 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0000 ns |     0.0000 ns |   0.0000 ns |   0.000 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,615.0328 ns |    84.1800 ns |   4.6142 ns | 108.859 |    0.56 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.2091 ns |     3.4983 ns |   0.1918 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,935.2244 ns |   295.8162 ns |  16.2147 ns |  88.388 |    0.61 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,857.7841 ns | 2,108.9990 ns | 115.6014 ns | 116.169 |    3.07 | 0.1678 |    2832 B |          NA |
