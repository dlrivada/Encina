
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                           | ShardCount | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |------------:|------:|------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **20,609.0 μs** |    **NA** |  **1.00** |    **7** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 18,569.7 μs |    NA |  0.90 |    5 |      48 B |        0.86 |
 'Directory routing'              | 3          | 17,810.5 μs |    NA |  0.86 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 21,078.6 μs |    NA |  1.02 |    8 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 18,248.3 μs |    NA |  0.89 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |    650.0 μs |    NA |  0.03 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 19,055.3 μs |    NA |  0.92 |    6 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 17,993.0 μs |    NA |  0.87 |    3 |     152 B |        2.71 |
                                  |            |             |       |       |      |           |             |
 **'Hash routing'**                   | **10**         | **17,898.1 μs** |    **NA** |  **1.00** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 10         | 18,605.4 μs |    NA |  1.04 |    7 |      48 B |        0.86 |
 'Directory routing'              | 10         | 17,446.0 μs |    NA |  0.97 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 10         | 20,447.0 μs |    NA |  1.14 |    8 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 10         | 17,988.5 μs |    NA |  1.01 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 10         |    629.6 μs |    NA |  0.04 |    1 |   12496 B |      223.14 |
 GetShardConnectionString         | 10         | 18,189.1 μs |    NA |  1.02 |    6 |      64 B |        1.14 |
 'Directory add + lookup'         | 10         | 17,941.5 μs |    NA |  1.00 |    4 |     152 B |        2.71 |
                                  |            |             |       |       |      |           |             |
 **'Hash routing'**                   | **50**         | **17,894.5 μs** |    **NA** |  **1.00** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 18,119.5 μs |    NA |  1.01 |    6 |      48 B |        0.86 |
 'Directory routing'              | 50         | 17,384.0 μs |    NA |  0.97 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 19,971.6 μs |    NA |  1.12 |    8 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 17,903.5 μs |    NA |  1.00 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |    610.1 μs |    NA |  0.03 |    1 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 18,364.4 μs |    NA |  1.03 |    7 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 17,848.2 μs |    NA |  1.00 |    3 |     152 B |        2.71 |
