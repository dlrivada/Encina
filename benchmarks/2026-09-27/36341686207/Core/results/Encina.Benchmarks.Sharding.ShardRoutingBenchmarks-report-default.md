
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error       | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|------------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.066 μs** |   **2.5968 μs** |  **0.1423 μs** | **3.016 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  1.925 μs |   0.7373 μs |  0.0404 μs | 1.948 μs |  0.63 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  2.581 μs |   2.4684 μs |  0.1353 μs | 2.564 μs |  0.84 |    0.05 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  3.756 μs |   3.6547 μs |  0.2003 μs | 3.656 μs |  1.23 |    0.07 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.142 μs |   8.1442 μs |  0.4464 μs | 6.062 μs |  2.01 |    0.15 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.848 μs |   5.6991 μs |  0.3124 μs | 3.731 μs |  1.26 |    0.10 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.279 μs |   3.1253 μs |  0.1713 μs | 3.196 μs |  1.07 |    0.06 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  5.410 μs |   8.5501 μs |  0.4687 μs | 5.160 μs |  1.77 |    0.15 |    3 |     152 B |        2.71 |
                                  |            |           |             |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **3.440 μs** |   **1.8455 μs** |  **0.1012 μs** | **3.387 μs** |  **1.00** |    **0.04** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.130 μs |   0.2212 μs |  0.0121 μs | 2.123 μs |  0.62 |    0.02 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.595 μs |   2.8771 μs |  0.1577 μs | 2.564 μs |  0.75 |    0.04 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  4.061 μs |   2.6459 μs |  0.1450 μs | 4.058 μs |  1.18 |    0.05 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 13.896 μs | 199.7962 μs | 10.9515 μs | 7.753 μs |  4.04 |    2.76 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  3.370 μs |   1.5220 μs |  0.0834 μs | 3.397 μs |  0.98 |    0.03 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  3.779 μs |   1.9782 μs |  0.1084 μs | 3.732 μs |  1.10 |    0.04 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  5.776 μs |   3.3541 μs |  0.1838 μs | 5.736 μs |  1.68 |    0.06 |    4 |     152 B |        2.71 |
