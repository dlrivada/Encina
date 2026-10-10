
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.244 μs** | **3.336 μs** | **0.1828 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.977 μs | 1.164 μs | 0.0638 μs |  0.61 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.947 μs | 6.117 μs | 0.3353 μs |  0.91 |    0.10 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.040 μs | 3.737 μs | 0.2049 μs |  1.25 |    0.08 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.084 μs | 5.200 μs | 0.2850 μs |  1.88 |    0.12 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.223 μs | 6.310 μs | 0.3459 μs |  1.00 |    0.10 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.226 μs | 2.051 μs | 0.1124 μs |  1.00 |    0.06 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.398 μs | 4.588 μs | 0.2515 μs |  1.67 |    0.10 |    4 |     152 B |        2.71 |
                                  |            |          |          |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.421 μs** | **2.033 μs** | **0.1114 μs** |  **1.00** |    **0.04** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.171 μs | 5.275 μs | 0.2892 μs |  0.64 |    0.08 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.568 μs | 1.117 μs | 0.0612 μs |  0.75 |    0.03 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.934 μs | 4.024 μs | 0.2205 μs |  1.15 |    0.06 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.848 μs | 6.321 μs | 0.3465 μs |  2.30 |    0.11 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.175 μs | 2.854 μs | 0.1565 μs |  0.93 |    0.05 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.790 μs | 4.454 μs | 0.2442 μs |  1.11 |    0.07 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.597 μs | 4.417 μs | 0.2421 μs |  1.64 |    0.08 |    4 |     152 B |        2.71 |
