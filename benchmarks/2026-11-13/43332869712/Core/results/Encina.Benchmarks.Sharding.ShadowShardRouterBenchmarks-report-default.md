
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.228 μs** | **3.863 μs** | **0.2117 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.418 μs | 5.248 μs | 0.2877 μs |  1.06 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.381 μs | 4.769 μs | 0.2614 μs |  1.36 |    0.11 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.623 μs | 7.266 μs | 0.3983 μs |  1.13 |    0.13 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.469 μs | 7.038 μs | 0.3858 μs |  1.08 |    0.12 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.822 μs** | **3.103 μs** | **0.1701 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.724 μs | 7.295 μs | 0.3999 μs |  0.98 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.441 μs | 4.414 μs | 0.2419 μs |  1.43 |    0.08 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.752 μs | 4.229 μs | 0.2318 μs |  0.98 |    0.06 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.313 μs | 2.107 μs | 0.1155 μs |  1.13 |    0.05 |    1 |      64 B |        1.14 |
