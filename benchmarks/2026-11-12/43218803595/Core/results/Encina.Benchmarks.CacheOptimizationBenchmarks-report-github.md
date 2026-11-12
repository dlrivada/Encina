```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    39.2776 ns |   7.4259 ns |  0.4070 ns |  0.931 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.8446 ns |   4.3239 ns |  0.2370 ns |  0.304 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.1648 ns |   0.3468 ns |  0.0190 ns |  0.004 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,053.4698 ns | 931.8889 ns | 51.0800 ns | 72.383 |    1.11 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    42.1861 ns |   4.3673 ns |  0.2394 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,610.5828 ns | 355.2509 ns | 19.4725 ns | 61.884 |    0.50 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,186.0221 ns |  91.7540 ns |  5.0293 ns | 75.525 |    0.39 | 0.0305 |    2832 B |          NA |
