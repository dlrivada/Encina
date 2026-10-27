
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **23.00 μs** |   **1.510 μs** |  **0.083 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.73 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.80 μs |   1.811 μs |  0.099 μs |   0.77 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.78 |
 'Complex predicates (parameterized)' | 2             |    23.88 μs |   1.007 μs |  0.055 μs |   1.04 |    0.00 |    3 | 0.2441 |   6.14 KB |        0.91 |
 'Two criteria (AND)'                 | 2             |    24.65 μs |   1.880 μs |  0.103 μs |   1.07 |    0.01 |    3 | 0.2441 |   6.14 KB |        0.91 |
 'Five criteria (AND)'                | 2             |    50.65 μs |   1.454 μs |  0.080 μs |   2.20 |    0.01 |    5 | 0.4272 |  11.55 KB |        1.72 |
 'Ten criteria (AND)'                 | 2             |    91.15 μs |   6.991 μs |  0.383 μs |   3.96 |    0.02 |    6 | 0.7324 |  20.91 KB |        3.11 |
 'Keyset pagination'                  | 2             |    37.70 μs |   1.286 μs |  0.070 μs |   1.64 |    0.01 |    4 | 0.3662 |  10.43 KB |        1.55 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,906.54 μs | 358.970 μs | 19.676 μs | 169.89 |    0.91 |    7 | 3.9063 | 109.83 KB |       16.33 |
 'Lambda Include'                     | 2             |    11.56 μs |   0.764 μs |  0.042 μs |   0.50 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.63 |
 'String Include'                     | 2             |    11.78 μs |   0.708 μs |  0.039 μs |   0.51 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 2             |    24.42 μs |   0.836 μs |  0.046 μs |   1.06 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 2             |    27.56 μs |   1.917 μs |  0.105 μs |   1.20 |    0.01 |    3 | 0.3662 |   9.36 KB |        1.39 |
 'Full specification (all features)'  | 2             |    83.08 μs |   9.753 μs |  0.535 μs |   3.61 |    0.02 |    6 | 0.7324 |  20.04 KB |        2.98 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.75 μs** |   **1.458 μs** |  **0.080 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.83 μs |   1.264 μs |  0.069 μs |   0.82 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    87.45 μs |   2.992 μs |  0.164 μs |   4.02 |    0.01 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.89 μs |   2.010 μs |  0.110 μs |   1.14 |    0.01 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.49 μs |   3.261 μs |  0.179 μs |   2.32 |    0.01 |    5 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    90.56 μs |   3.465 μs |  0.190 μs |   4.16 |    0.02 |    6 | 0.8545 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 10            |    37.67 μs |   2.550 μs |  0.140 μs |   1.73 |    0.01 |    4 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,984.53 μs |  54.379 μs |  2.981 μs | 183.16 |    0.59 |    7 |      - | 109.63 KB |       17.01 |
 'Lambda Include'                     | 10            |    11.81 μs |   0.519 μs |  0.028 μs |   0.54 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.96 μs |   0.590 μs |  0.032 μs |   0.55 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.57 μs |   1.009 μs |  0.055 μs |   1.13 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    28.05 μs |   1.018 μs |  0.056 μs |   1.29 |    0.00 |    3 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    81.64 μs |   3.646 μs |  0.200 μs |   3.75 |    0.01 |    6 | 0.7324 |  19.88 KB |        3.08 |
