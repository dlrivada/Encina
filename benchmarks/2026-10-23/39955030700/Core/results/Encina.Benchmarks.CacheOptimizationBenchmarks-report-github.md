```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    28.5907 ns |   0.2439 ns |  0.0134 ns |  0.96 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    10.6839 ns |  14.3054 ns |  0.7841 ns |  0.36 |    0.02 |      - |         - |          NA |
| TypeCheck_Direct               |     0.5247 ns |   2.4879 ns |  0.1364 ns |  0.02 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,386.1819 ns | 118.4161 ns |  6.4908 ns | 79.83 |    0.19 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    29.8892 ns |   0.2897 ns |  0.0159 ns |  1.00 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,001.2198 ns | 250.7380 ns | 13.7438 ns | 66.95 |    0.40 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,538.6074 ns | 463.6310 ns | 25.4132 ns | 84.93 |    0.74 | 0.0305 |    2832 B |          NA |
