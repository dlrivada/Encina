
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error       | StdDev     | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|------------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **5.146 μs** |  **42.3704 μs** |  **2.3225 μs** |  **4.187 μs** |  **1.13** |    **0.59** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  2.203 μs |   5.6272 μs |  0.3084 μs |  2.123 μs |  0.48 |    0.17 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  2.982 μs |   6.0164 μs |  0.3298 μs |  2.856 μs |  0.65 |    0.22 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  4.832 μs |  16.0596 μs |  0.8803 μs |  4.498 μs |  1.06 |    0.39 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.255 μs |   8.3194 μs |  0.4560 μs |  6.081 μs |  1.37 |    0.46 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  4.014 μs |  24.4328 μs |  1.3392 μs |  3.255 μs |  0.88 |    0.39 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  4.859 μs |  29.6213 μs |  1.6236 μs |  4.468 μs |  1.06 |    0.47 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  6.435 μs |   3.5451 μs |  0.1943 μs |  6.531 μs |  1.41 |    0.46 |    4 |     152 B |        2.71 |
                                  |            |           |             |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **3.868 μs** |   **5.5708 μs** |  **0.3054 μs** |  **3.727 μs** |  **1.00** |    **0.10** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.444 μs |   0.8360 μs |  0.0458 μs |  2.434 μs |  0.63 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  5.112 μs |  27.7000 μs |  1.5183 μs |  5.149 μs |  1.33 |    0.35 |    3 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 11.228 μs | 206.5766 μs | 11.3232 μs |  5.070 μs |  2.91 |    2.56 |    5 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 10.947 μs |  15.9029 μs |  0.8717 μs | 10.837 μs |  2.84 |    0.27 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  4.317 μs |  14.6288 μs |  0.8019 μs |  4.087 μs |  1.12 |    0.19 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  4.996 μs |   6.0764 μs |  0.3331 μs |  4.948 μs |  1.30 |    0.11 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  5.898 μs |   6.8587 μs |  0.3759 μs |  6.002 μs |  1.53 |    0.13 |    3 |     152 B |        2.71 |
