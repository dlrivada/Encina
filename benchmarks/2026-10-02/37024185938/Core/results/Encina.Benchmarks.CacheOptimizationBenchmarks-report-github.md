```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean          | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    43.6614 ns |   2.9308 ns |  0.1606 ns |  0.952 |    0.00 |      - |         - |          NA |
| TypeCheck_Cached               |    12.4922 ns |   0.0373 ns |  0.0020 ns |  0.272 |    0.00 |      - |         - |          NA |
| TypeCheck_Direct               |     0.3492 ns |   0.0172 ns |  0.0009 ns |  0.008 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,944.2222 ns | 294.5130 ns | 16.1433 ns | 86.028 |    0.31 | 0.1526 |    2624 B |          NA |
| Cache_GetOrAdd_Direct          |    45.8479 ns |   0.1412 ns |  0.0077 ns |  1.000 |    0.00 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 3,180.6743 ns |  43.4932 ns |  2.3840 ns | 69.375 |    0.05 | 0.1106 |    1896 B |          NA |
| Send_Query_CacheHit            | 4,073.0065 ns | 110.8098 ns |  6.0739 ns | 88.837 |    0.12 | 0.1526 |    2600 B |          NA |
