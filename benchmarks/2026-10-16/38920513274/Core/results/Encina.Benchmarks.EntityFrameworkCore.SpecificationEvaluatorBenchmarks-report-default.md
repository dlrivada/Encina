
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.08 μs** |   **1.198 μs** |  **0.066 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.02 μs |   0.738 μs |  0.040 μs |   0.82 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.15 μs |   1.446 μs |  0.079 μs |   1.09 |    0.00 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.62 μs |   1.283 μs |  0.070 μs |   1.11 |    0.00 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    51.87 μs |   1.244 μs |  0.068 μs |   2.35 |    0.01 |    5 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.54 μs |   3.878 μs |  0.213 μs |   4.01 |    0.01 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    37.99 μs |   1.219 μs |  0.067 μs |   1.72 |    0.01 |    4 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,944.09 μs | 787.396 μs | 43.160 μs | 178.61 |    1.75 |    7 | 3.9063 | 109.04 KB |       16.92 |
 'Lambda Include'                     | 2             |    11.75 μs |   0.187 μs |  0.010 μs |   0.53 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    12.04 μs |   0.771 μs |  0.042 μs |   0.55 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.59 μs |   1.495 μs |  0.082 μs |   1.11 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    29.39 μs |   4.626 μs |  0.254 μs |   1.33 |    0.01 |    3 | 0.3662 |   9.66 KB |        1.50 |
 'Full specification (all features)'  | 2             |    82.21 μs |  10.709 μs |  0.587 μs |   3.72 |    0.02 |    6 | 0.7324 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.08 μs** |   **1.294 μs** |  **0.071 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.85 μs |   0.795 μs |  0.044 μs |   0.81 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    86.95 μs |   6.009 μs |  0.329 μs |   3.94 |    0.02 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.54 μs |   1.011 μs |  0.055 μs |   1.11 |    0.00 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.46 μs |   1.741 μs |  0.095 μs |   2.28 |    0.01 |    5 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.26 μs |   6.926 μs |  0.380 μs |   3.95 |    0.02 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    37.72 μs |   0.789 μs |  0.043 μs |   1.71 |    0.01 |    4 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,942.42 μs | 336.184 μs | 18.427 μs | 178.53 |    0.88 |    7 | 3.9063 | 109.24 KB |       16.95 |
 'Lambda Include'                     | 10            |    11.79 μs |   0.347 μs |  0.019 μs |   0.53 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.70 μs |   0.665 μs |  0.036 μs |   0.53 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.10 μs |   0.642 μs |  0.035 μs |   1.14 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    27.69 μs |   1.034 μs |  0.057 μs |   1.25 |    0.00 |    3 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    82.24 μs |   3.994 μs |  0.219 μs |   3.72 |    0.01 |    6 | 0.7324 |  19.88 KB |        3.08 |
