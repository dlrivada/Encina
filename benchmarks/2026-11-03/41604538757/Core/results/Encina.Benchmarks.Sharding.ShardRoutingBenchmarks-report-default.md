
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error       | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|------------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.907 μs** |   **8.2727 μs** |  **0.4535 μs** | **3.847 μs** |  **1.01** |    **0.14** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  1.963 μs |   0.9757 μs |  0.0535 μs | 1.943 μs |  0.51 |    0.05 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  2.802 μs |   3.7774 μs |  0.2071 μs | 2.896 μs |  0.72 |    0.09 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  3.814 μs |   4.7455 μs |  0.2601 μs | 3.758 μs |  0.99 |    0.11 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.115 μs |   3.9410 μs |  0.2160 μs | 6.112 μs |  1.58 |    0.16 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.302 μs |   1.3954 μs |  0.0765 μs | 3.285 μs |  0.85 |    0.09 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.319 μs |   2.8285 μs |  0.1550 μs | 3.315 μs |  0.86 |    0.09 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  6.291 μs |   8.4546 μs |  0.4634 μs | 6.502 μs |  1.62 |    0.19 |    3 |     152 B |        2.71 |
                                  |            |           |             |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **3.479 μs** |   **3.0424 μs** |  **0.1668 μs** | **3.455 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 10.378 μs | 256.6901 μs | 14.0700 μs | 2.280 μs |  2.99 |    3.51 |    5 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.609 μs |   1.0272 μs |  0.0563 μs | 2.625 μs |  0.75 |    0.03 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  4.081 μs |   1.0690 μs |  0.0586 μs | 4.058 μs |  1.18 |    0.05 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         |  7.888 μs |   5.9060 μs |  0.3237 μs | 7.764 μs |  2.27 |    0.12 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  3.693 μs |   8.5331 μs |  0.4677 μs | 3.497 μs |  1.06 |    0.12 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  4.061 μs |   6.8852 μs |  0.3774 μs | 4.017 μs |  1.17 |    0.11 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  5.480 μs |   4.0376 μs |  0.2213 μs | 5.399 μs |  1.58 |    0.09 |    3 |     152 B |        2.71 |
