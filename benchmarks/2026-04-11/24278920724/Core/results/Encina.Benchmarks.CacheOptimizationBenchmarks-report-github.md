```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                         | Mean          | Error      | StdDev     | Median        | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |--------------:|-----------:|-----------:|--------------:|------:|--------:|-------:|----------:|------------:|
| Cache_TryGetValue_ThenGetOrAdd |    42.8873 ns |  1.7247 ns |  2.5814 ns |    41.7167 ns |  1.05 |    0.07 |      - |         - |          NA |
| TypeCheck_Cached               |    11.9496 ns |  0.4082 ns |  0.5855 ns |    12.4762 ns |  0.29 |    0.02 |      - |         - |          NA |
| TypeCheck_Direct               |     0.6192 ns |  0.0304 ns |  0.0446 ns |     0.6361 ns |  0.02 |    0.00 |      - |         - |          NA |
| Send_Command_CacheHit          | 3,461.0439 ns |  7.5118 ns | 11.2434 ns | 3,461.8522 ns | 84.92 |    3.20 | 0.0877 |    2272 B |          NA |
| Cache_GetOrAdd_Direct          |    40.8159 ns |  1.0655 ns |  1.5618 ns |    41.7216 ns |  1.00 |    0.05 |      - |         - |          NA |
| Publish_Notification_CacheHit  | 1,987.6930 ns | 22.8859 ns | 34.2546 ns | 1,986.5549 ns | 48.77 |    2.01 | 0.0534 |    1384 B |          NA |
| Send_Query_CacheHit            | 3,454.1814 ns |  8.6883 ns | 13.0042 ns | 3,453.3410 ns | 84.75 |    3.20 | 0.0877 |    2248 B |          NA |
