```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    34.1852 ns |   8.9154 ns |  0.4887 ns |   1.044 |    0.01 |      - |         - |          NA |
| TypeCheck_Cached               |    10.4578 ns |   2.5491 ns |  0.1397 ns |   0.319 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.2997 ns |   0.2731 ns |  0.0150 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,623.1240 ns | 464.3227 ns | 25.4511 ns | 110.635 |    0.69 | 0.1678 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    32.7486 ns |   1.0564 ns |  0.0579 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,936.9254 ns | 493.4693 ns | 27.0487 ns |  89.681 |    0.73 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 3,749.0346 ns | 213.3072 ns | 11.6921 ns | 114.480 |    0.36 | 0.1678 |    2832 B |          NA |
