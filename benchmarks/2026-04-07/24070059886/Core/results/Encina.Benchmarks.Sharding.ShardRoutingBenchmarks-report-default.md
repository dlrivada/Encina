
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                           | ShardCount | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |------------:|------:|------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **17,719.3 μs** |    **NA** |  **1.00** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 18,172.2 μs |    NA |  1.03 |    5 |      48 B |        0.86 |
 'Directory routing'              | 3          | 17,084.3 μs |    NA |  0.96 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 20,489.8 μs |    NA |  1.16 |    8 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 18,239.4 μs |    NA |  1.03 |    6 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |    651.0 μs |    NA |  0.04 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 18,609.2 μs |    NA |  1.05 |    7 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 17,975.6 μs |    NA |  1.01 |    4 |     152 B |        2.71 |
                                  |            |             |       |       |      |           |             |
 **'Hash routing'**                   | **50**         | **17,908.8 μs** |    **NA** |  **1.00** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 18,290.3 μs |    NA |  1.02 |    6 |      48 B |        0.86 |
 'Directory routing'              | 50         | 17,324.7 μs |    NA |  0.97 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 20,010.2 μs |    NA |  1.12 |    8 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 17,815.1 μs |    NA |  0.99 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |    613.3 μs |    NA |  0.03 |    1 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 18,557.1 μs |    NA |  1.04 |    7 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 18,158.5 μs |    NA |  1.01 |    5 |     152 B |        2.71 |
