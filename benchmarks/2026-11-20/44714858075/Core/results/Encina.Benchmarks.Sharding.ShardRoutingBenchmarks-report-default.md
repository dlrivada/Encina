
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **4.904 μs** |  **22.639 μs** | **1.2409 μs** | **5.271 μs** |  **1.05** |    **0.35** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  1.306 μs |  14.541 μs | 0.7970 μs | 1.315 μs |  0.28 |    0.17 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  5.336 μs |  17.107 μs | 0.9377 μs | 5.673 μs |  1.14 |    0.34 |    3 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  4.855 μs |  13.276 μs | 0.7277 μs | 4.715 μs |  1.04 |    0.29 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.109 μs |  37.330 μs | 2.0462 μs | 4.944 μs |  1.31 |    0.51 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  2.792 μs |  37.767 μs | 2.0701 μs | 1.635 μs |  0.60 |    0.42 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  2.572 μs |  24.428 μs | 1.3390 μs | 2.077 μs |  0.55 |    0.29 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  4.422 μs |  47.898 μs | 2.6254 μs | 4.698 μs |  0.95 |    0.55 |    3 |     152 B |        2.71 |
                                  |            |           |            |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **4.385 μs** |   **4.880 μs** | **0.2675 μs** | **4.352 μs** |  **1.00** |    **0.07** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  3.257 μs |   2.217 μs | 0.1215 μs | 3.269 μs |  0.74 |    0.05 |    2 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.730 μs |  10.435 μs | 0.5720 μs | 2.607 μs |  0.62 |    0.12 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  5.888 μs |  21.431 μs | 1.1747 μs | 5.505 μs |  1.35 |    0.24 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 11.741 μs | 108.952 μs | 5.9720 μs | 8.966 μs |  2.68 |    1.19 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  5.892 μs |  14.591 μs | 0.7998 μs | 5.831 μs |  1.35 |    0.17 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  4.926 μs |   8.351 μs | 0.4577 μs | 4.973 μs |  1.13 |    0.11 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  9.801 μs | 110.433 μs | 6.0532 μs | 6.947 μs |  2.24 |    1.21 |    3 |     152 B |        2.71 |
