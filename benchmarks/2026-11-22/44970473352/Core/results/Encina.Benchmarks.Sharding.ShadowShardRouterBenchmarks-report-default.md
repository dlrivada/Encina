
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.069 μs** | **0.0633 μs** | **0.0927 μs** |  **1.00** |    **0.04** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.246 μs | 0.2025 μs | 0.2969 μs |  1.06 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.166 μs | 0.0841 μs | 0.1206 μs |  1.36 |    0.06 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.189 μs | 0.0810 μs | 0.1082 μs |  1.04 |    0.05 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.278 μs | 0.0515 μs | 0.0722 μs |  1.07 |    0.04 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.343 μs** | **0.0889 μs** | **0.1217 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.654 μs | 0.2277 μs | 0.3265 μs |  1.09 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.418 μs | 0.2602 μs | 0.3731 μs |  1.62 |    0.12 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.074 μs | 0.2968 μs | 0.4351 μs |  1.22 |    0.14 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.460 μs | 0.2348 μs | 0.3367 μs |  1.34 |    0.11 |    1 |      64 B |        1.14 |
