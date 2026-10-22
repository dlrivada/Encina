```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    35.5495 ns |  0.0697 ns |  0.1022 ns |    35.5509 ns |  0.901 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    11.4187 ns |  0.4096 ns |  0.5742 ns |    11.0097 ns |  0.290 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3117 ns |  0.0110 ns |  0.0165 ns |     0.3122 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,735.1357 ns | 12.7349 ns | 18.2640 ns | 2,732.8988 ns | 69.345 |    1.10 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    39.4510 ns |  0.3963 ns |  0.5932 ns |    39.2644 ns |  1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,474.4090 ns | 16.3088 ns | 23.3896 ns | 2,474.4492 ns | 62.734 |    1.08 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,931.7614 ns | 29.0011 ns | 42.5095 ns | 2,947.7691 ns | 74.330 |    1.51 | 0.0305 |    2832 B |          NA |
