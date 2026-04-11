```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.5324 ns |  0.3922 ns |  0.5871 ns |    43.4618 ns |  1.005 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |    12.6467 ns |  0.2043 ns |  0.3057 ns |    12.6163 ns |  0.292 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.0459 ns |  0.0790 ns |  0.1159 ns |     0.0000 ns |  0.001 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,782.3046 ns | 18.4424 ns | 26.4496 ns | 3,780.9407 ns | 87.354 |    1.19 | 0.1335 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    43.3045 ns |  0.3547 ns |  0.5200 ns |    43.3708 ns |  1.000 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,981.7557 ns | 42.2103 ns | 63.1784 ns | 1,987.7985 ns | 45.770 |    1.53 | 0.0801 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,919.8412 ns |  8.8071 ns | 13.1821 ns | 3,921.3000 ns | 90.531 |    1.11 | 0.1297 |    2248 B |          NA |
