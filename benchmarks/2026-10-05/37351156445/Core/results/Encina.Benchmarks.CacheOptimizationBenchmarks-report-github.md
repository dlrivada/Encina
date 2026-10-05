```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    41.6957 ns |   0.3154 ns |  0.0173 ns |   0.942 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5215 ns |   0.0966 ns |  0.0053 ns |   0.283 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3817 ns |   0.0766 ns |  0.0042 ns |   0.009 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,273.4155 ns | 488.3326 ns | 26.7672 ns |  96.500 |    0.53 | 0.1755 |    2952 B |          NA |
| Cache_GetOrAdd_Direct          |    44.2843 ns |   0.5325 ns |  0.0292 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,568.2902 ns | 170.9148 ns |  9.3684 ns |  80.577 |    0.19 | 0.1297 |    2224 B |          NA |
| Send_Query_CacheHit            | 4,587.7356 ns | 280.2797 ns | 15.3631 ns | 103.597 |    0.31 | 0.1678 |    2928 B |          NA |
