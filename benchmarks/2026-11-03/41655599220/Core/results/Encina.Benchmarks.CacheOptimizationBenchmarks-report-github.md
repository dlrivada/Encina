```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    34.3264 ns |   0.2188 ns |  0.0120 ns |   1.027 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.3721 ns |   0.9367 ns |  0.0513 ns |   0.310 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0000 ns |   0.0000 ns |  0.0000 ns |   0.000 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,787.7485 ns | 249.3839 ns | 13.6696 ns | 113.354 |    0.54 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    33.4156 ns |   2.5317 ns |  0.1388 ns |   1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,942.9502 ns | 170.0047 ns |  9.3185 ns |  88.072 |    0.40 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,758.6977 ns | 964.6497 ns | 52.8757 ns | 112.484 |    1.43 | 0.1678 |    2832 B |          NA |
