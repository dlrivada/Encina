```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.6298 ns |  0.1056 ns |  0.1581 ns |  0.974 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1447 ns |  0.0059 ns |  0.0081 ns |  0.271 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2714 ns |  0.0011 ns |  0.0015 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,614.2065 ns | 20.8155 ns | 31.1556 ns | 80.713 |    0.82 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    44.7801 ns |  0.1726 ns |  0.2530 ns |  1.000 |    0.01 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,977.9154 ns |  7.7633 ns | 11.3793 ns | 44.171 |    0.35 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,834.0439 ns | 20.3014 ns | 30.3862 ns | 85.622 |    0.82 | 0.1297 |    2248 B |          NA |
