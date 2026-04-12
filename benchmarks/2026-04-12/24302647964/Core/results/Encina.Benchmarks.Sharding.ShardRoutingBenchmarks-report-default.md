
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.045 μs** | **0.0807 μs** | **0.1157 μs** | **3.053 μs** |  **1.00** |    **0.05** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.950 μs | 0.0532 μs | 0.0745 μs | 1.939 μs |  0.64 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.596 μs | 0.0512 μs | 0.0766 μs | 2.585 μs |  0.85 |    0.04 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.689 μs | 0.0860 μs | 0.1206 μs | 3.698 μs |  1.21 |    0.06 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.249 μs | 0.4003 μs | 0.5868 μs | 6.052 μs |  2.06 |    0.20 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.351 μs | 0.2421 μs | 0.3314 μs | 3.308 μs |  1.10 |    0.11 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.408 μs | 0.0631 μs | 0.0885 μs | 3.406 μs |  1.12 |    0.05 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.714 μs | 0.2562 μs | 0.3755 μs | 5.661 μs |  1.88 |    0.14 |    5 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.303 μs** | **0.0855 μs** | **0.1226 μs** | **3.302 μs** |  **1.00** |    **0.05** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.368 μs | 0.1866 μs | 0.2793 μs | 2.280 μs |  0.72 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.654 μs | 0.0595 μs | 0.0814 μs | 2.655 μs |  0.80 |    0.04 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.314 μs | 0.2661 μs | 0.3901 μs | 4.583 μs |  1.31 |    0.13 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.243 μs | 0.4709 μs | 0.6903 μs | 8.230 μs |  2.50 |    0.23 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.219 μs | 0.0638 μs | 0.0916 μs | 3.226 μs |  0.98 |    0.05 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.865 μs | 0.0356 μs | 0.0511 μs | 3.868 μs |  1.17 |    0.05 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.961 μs | 0.2660 μs | 0.3981 μs | 5.851 μs |  1.81 |    0.14 |    4 |     152 B |        2.71 |
