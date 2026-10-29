```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    40.7028 ns |   1.6192 ns |  0.0888 ns |  0.934 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    14.0302 ns |   0.2317 ns |  0.0127 ns |  0.322 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2762 ns |   0.1741 ns |  0.0095 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,281.2314 ns | 137.2216 ns |  7.5216 ns | 75.310 |    0.20 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    43.5697 ns |   1.5404 ns |  0.0844 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,749.9009 ns | 280.4775 ns | 15.3739 ns | 63.115 |    0.32 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,488.8983 ns | 190.8936 ns | 10.4635 ns | 80.076 |    0.25 | 0.0305 |    2832 B |          NA |
