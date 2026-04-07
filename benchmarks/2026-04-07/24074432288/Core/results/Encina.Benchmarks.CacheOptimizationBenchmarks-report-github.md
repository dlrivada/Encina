```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.6438 ns |  0.0372 ns |  0.0510 ns |    43.6477 ns |  0.962 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    12.1476 ns |  0.0132 ns |  0.0171 ns |    12.1438 ns |  0.268 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2802 ns |  0.0094 ns |  0.0135 ns |     0.2759 ns |  0.006 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,698.0828 ns |  9.5999 ns | 14.0714 ns | 3,694.5991 ns | 81.485 |    1.18 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    45.3923 ns |  0.4605 ns |  0.6456 ns |    45.7598 ns |  1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,013.9711 ns |  7.9154 ns | 10.5669 ns | 2,018.8637 ns | 44.377 |    0.66 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,858.6675 ns | 17.3792 ns | 25.4742 ns | 3,863.6327 ns | 85.024 |    1.31 | 0.1297 |    2248 B |          NA |
