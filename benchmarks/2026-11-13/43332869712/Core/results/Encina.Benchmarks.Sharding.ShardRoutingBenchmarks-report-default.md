
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **2.248 μs** |  **24.271 μs** |  **1.3304 μs** | **2.285 μs** |  **1.37** |    **1.19** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  4.413 μs |  21.653 μs |  1.1869 μs | 4.490 μs |  2.69 |    1.84 |    4 |      48 B |        0.86 |
 'Directory routing'              | 3          |  3.278 μs |  11.069 μs |  0.6067 μs | 3.338 μs |  2.00 |    1.31 |    3 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  2.754 μs |  30.309 μs |  1.6614 μs | 2.374 μs |  1.68 |    1.47 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  8.321 μs |   8.930 μs |  0.4895 μs | 8.429 μs |  5.08 |    3.20 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  2.434 μs |  29.071 μs |  1.5935 μs | 1.518 μs |  1.49 |    1.35 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  5.411 μs |  30.770 μs |  1.6866 μs | 5.051 μs |  3.30 |    2.31 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  5.432 μs |  51.310 μs |  2.8125 μs | 5.127 μs |  3.32 |    2.70 |    4 |     152 B |        2.71 |
                                  |            |           |            |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **3.563 μs** |  **15.924 μs** |  **0.8729 μs** | **3.252 μs** |  **1.04** |    **0.30** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  3.767 μs |  21.218 μs |  1.1630 μs | 3.374 μs |  1.10 |    0.37 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  4.231 μs |   8.571 μs |  0.4698 μs | 4.383 μs |  1.23 |    0.27 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  6.295 μs |  21.179 μs |  1.1609 μs | 6.949 μs |  1.83 |    0.46 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 12.080 μs | 102.842 μs |  5.6371 μs | 9.094 μs |  3.52 |    1.60 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  6.144 μs |  36.661 μs |  2.0095 μs | 5.095 μs |  1.79 |    0.62 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  6.835 μs |   8.364 μs |  0.4585 μs | 6.966 μs |  1.99 |    0.40 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 13.523 μs | 261.132 μs | 14.3135 μs | 6.223 μs |  3.94 |    3.75 |    5 |     152 B |        2.71 |
