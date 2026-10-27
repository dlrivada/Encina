```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    38.2143 ns |   4.8691 ns |  0.2669 ns |  0.960 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.8962 ns |   2.5783 ns |  0.1413 ns |  0.274 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0877 ns |   0.2425 ns |  0.0133 ns |  0.002 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,839.1209 ns | 377.6458 ns | 20.7000 ns | 71.347 |    0.63 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    39.7944 ns |   5.2271 ns |  0.2865 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,288.8698 ns | 225.6250 ns | 12.3673 ns | 57.519 |    0.45 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,987.9968 ns | 545.7161 ns | 29.9125 ns | 75.088 |    0.80 | 0.0305 |    2832 B |          NA |
