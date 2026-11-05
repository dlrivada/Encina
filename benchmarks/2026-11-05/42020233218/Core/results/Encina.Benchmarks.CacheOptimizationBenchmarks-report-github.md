```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|--------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    37.8074 ns |     0.3323 ns |  0.0182 ns |   0.99 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    11.3232 ns |     0.1493 ns |  0.0082 ns |   0.30 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.4771 ns |     0.2513 ns |  0.0138 ns |   0.01 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,955.8757 ns |   221.1866 ns | 12.1240 ns | 103.87 |    0.28 | 0.1068 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    38.0840 ns |     0.5104 ns |  0.0280 ns |   1.00 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,216.6246 ns | 1,501.6077 ns | 82.3082 ns |  84.46 |    1.87 | 0.0877 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,144.0120 ns |    62.6782 ns |  3.4356 ns | 108.81 |    0.10 | 0.1068 |    2832 B |          NA |
