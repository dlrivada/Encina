```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Median        | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    32.9654 ns |   3.1343 ns |  0.1718 ns |    32.9695 ns |   0.986 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.2957 ns |   0.8098 ns |  0.0444 ns |    10.3095 ns |   0.308 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0023 ns |   0.0739 ns |  0.0041 ns |     0.0000 ns |   0.000 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,512.7090 ns |  94.9634 ns |  5.2053 ns | 3,510.9841 ns | 105.064 |    0.89 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.4362 ns |   5.9100 ns |  0.3239 ns |    33.3797 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,865.8209 ns | 155.4873 ns |  8.5228 ns | 2,864.6648 ns |  85.715 |    0.75 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,667.0701 ns | 521.1954 ns | 28.5685 ns | 3,653.3335 ns | 109.680 |    1.18 | 0.1678 |    2832 B |          NA |
