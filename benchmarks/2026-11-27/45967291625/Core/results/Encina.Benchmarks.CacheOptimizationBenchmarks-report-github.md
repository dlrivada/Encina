```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.34GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    40.4241 ns |   5.6703 ns |  0.3108 ns |  0.985 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    13.4261 ns |   1.0861 ns |  0.0595 ns |  0.327 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3972 ns |   0.5016 ns |  0.0275 ns |  0.010 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,122.0610 ns | 176.3328 ns |  9.6654 ns | 76.054 |    0.89 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    41.0553 ns |   9.9126 ns |  0.5433 ns |  1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,584.4358 ns | 213.1395 ns | 11.6829 ns | 62.957 |    0.76 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,352.5522 ns | 453.7077 ns | 24.8692 ns | 81.669 |    1.07 | 0.0305 |    2832 B |          NA |
