
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.278 μs** | **0.0626 μs** | **0.0898 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.153 μs | 0.0842 μs | 0.1207 μs |  0.95 |    0.06 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.019 μs | 0.1247 μs | 0.1789 μs |  1.33 |    0.09 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.228 μs | 0.1209 μs | 0.1734 μs |  0.98 |    0.08 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.418 μs | 0.1793 μs | 0.2571 μs |  1.06 |    0.12 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.933 μs** | **0.1911 μs** | **0.2801 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.077 μs | 0.5375 μs | 0.7709 μs |  1.06 |    0.28 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 3.630 μs | 0.1327 μs | 0.1903 μs |  1.25 |    0.13 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.619 μs | 0.0962 μs | 0.1348 μs |  0.90 |    0.10 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.209 μs | 0.3497 μs | 0.5125 μs |  1.10 |    0.20 |    1 |      64 B |        1.14 |
