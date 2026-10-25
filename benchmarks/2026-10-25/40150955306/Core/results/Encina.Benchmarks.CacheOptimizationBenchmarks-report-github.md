```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    28.8969 ns |  0.1210 ns |  0.1696 ns |    28.7899 ns |  0.97 |    0.02 |      - |         - |          NA |
| TypeCheck_Cached               |     9.5824 ns |  0.0783 ns |  0.1072 ns |     9.5644 ns |  0.32 |    0.01 |      - |         - |          NA |
| TypeCheck_Direct               |     0.5289 ns |  0.0148 ns |  0.0203 ns |     0.5288 ns |  0.02 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 2,433.1089 ns | 16.6223 ns | 23.8393 ns | 2,430.4866 ns | 81.86 |    1.48 | 0.0305 |    2856 B |          NA |
| Cache_GetOrAdd_Direct          |    29.7306 ns |  0.3435 ns |  0.4816 ns |    29.5487 ns |  1.00 |    0.02 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 2,009.8792 ns | 28.5400 ns | 40.9312 ns | 2,014.4650 ns | 67.62 |    1.70 | 0.0229 |    2232 B |          NA |
| Send_Query_CacheHit            | 2,538.1875 ns | 15.7572 ns | 22.0894 ns | 2,531.1822 ns | 85.39 |    1.50 | 0.0305 |    2832 B |          NA |
