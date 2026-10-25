
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.569 μs** | **2.920 μs** | **0.1600 μs** |  **1.00** |    **0.06** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.994 μs | 1.751 μs | 0.0960 μs |  0.56 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.492 μs | 1.872 μs | 0.1026 μs |  0.70 |    0.04 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.808 μs | 3.047 μs | 0.1670 μs |  1.07 |    0.06 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.309 μs | 5.011 μs | 0.2747 μs |  1.77 |    0.10 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.242 μs | 1.364 μs | 0.0748 μs |  0.91 |    0.04 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.356 μs | 2.286 μs | 0.1253 μs |  0.94 |    0.05 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.152 μs | 4.946 μs | 0.2711 μs |  1.73 |    0.09 |    4 |     152 B |        2.71 |
                                  |            |          |          |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.624 μs** | **2.423 μs** | **0.1328 μs** |  **1.00** |    **0.04** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.194 μs | 1.766 μs | 0.0968 μs |  0.61 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.629 μs | 1.581 μs | 0.0867 μs |  0.73 |    0.03 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.028 μs | 3.435 μs | 0.1883 μs |  1.11 |    0.06 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.658 μs | 4.360 μs | 0.2390 μs |  2.12 |    0.09 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.445 μs | 3.623 μs | 0.1986 μs |  0.95 |    0.06 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.014 μs | 7.496 μs | 0.4109 μs |  1.11 |    0.10 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.567 μs | 4.081 μs | 0.2237 μs |  1.54 |    0.07 |    3 |     152 B |        2.71 |
