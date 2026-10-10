```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio   | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|--------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.6975 ns |   0.3951 ns |  0.0217 ns |   0.995 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.5058 ns |   0.4047 ns |  0.0222 ns |   0.291 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3636 ns |   0.3980 ns |  0.0218 ns |   0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 4,267.8740 ns | 312.5306 ns | 17.1309 ns |  99.433 |    0.35 | 0.1755 |    2960 B |          NA |
| Cache_GetOrAdd_Direct          |    42.9222 ns |   0.5317 ns |  0.0291 ns |   1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,598.4567 ns | 204.1129 ns | 11.1881 ns |  83.837 |    0.23 | 0.1297 |    2232 B |          NA |
| Send_Query_CacheHit            | 4,431.5227 ns | 380.1985 ns | 20.8400 ns | 103.246 |    0.42 | 0.1755 |    2936 B |          NA |
