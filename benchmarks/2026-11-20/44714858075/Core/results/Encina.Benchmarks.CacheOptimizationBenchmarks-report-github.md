```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.1602 ns |   6.9159 ns |  0.3791 ns |  0.959 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    14.7158 ns |   0.7927 ns |  0.0434 ns |  0.327 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3788 ns |   0.2394 ns |  0.0131 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,383.5656 ns | 110.9556 ns |  6.0819 ns | 75.185 |    0.43 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    45.0041 ns |   5.2023 ns |  0.2852 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,772.4553 ns | 178.7369 ns |  9.7972 ns | 61.606 |    0.39 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,566.7111 ns | 336.1997 ns | 18.4282 ns | 79.255 |    0.56 | 0.0305 |    2832 B |          NA |
