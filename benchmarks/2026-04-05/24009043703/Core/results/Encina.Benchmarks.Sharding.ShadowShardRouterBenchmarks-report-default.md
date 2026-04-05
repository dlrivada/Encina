
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                                   | ShardCount | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |------------:|------:|------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **17,735.4 μs** |    **NA** |  **1.00** |    **3** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 17,794.5 μs |    NA |  1.00 |    4 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 16,491.5 μs |    NA |  0.93 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          |    632.2 μs |    NA |  0.04 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 18,794.9 μs |    NA |  1.06 |    5 |      64 B |        1.14 |
                                          |            |             |       |       |      |           |             |
 **'Bare HashRouter'**                        | **10**         | **17,695.4 μs** |    **NA** |  **1.00** |    **4** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 10         | 17,674.7 μs |    NA |  1.00 |    3 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 10         | 16,551.8 μs |    NA |  0.94 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 10         |    698.9 μs |    NA |  0.04 |    1 |     160 B |        2.86 |
 'Decorated GetShardConnectionString'     | 10         | 18,420.4 μs |    NA |  1.04 |    5 |      64 B |        1.14 |
                                          |            |             |       |       |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **17,748.4 μs** |    **NA** |  **1.00** |    **4** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 17,716.8 μs |    NA |  1.00 |    3 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 16,552.2 μs |    NA |  0.93 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         |    669.0 μs |    NA |  0.04 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 18,013.1 μs |    NA |  1.01 |    5 |      64 B |        1.14 |
