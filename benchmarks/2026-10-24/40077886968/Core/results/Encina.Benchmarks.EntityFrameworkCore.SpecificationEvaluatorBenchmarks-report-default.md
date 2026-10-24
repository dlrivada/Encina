
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.93 μs** |   **0.749 μs** |  **0.041 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.15 μs |   1.626 μs |  0.089 μs |   0.83 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.73 μs |   5.219 μs |  0.286 μs |   1.13 |    0.01 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.65 μs |   2.467 μs |  0.135 μs |   1.12 |    0.01 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.52 μs |   2.273 μs |  0.125 μs |   2.30 |    0.01 |    5 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    87.53 μs |  15.662 μs |  0.859 μs |   3.99 |    0.03 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    37.54 μs |   3.025 μs |  0.166 μs |   1.71 |    0.01 |    4 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,107.28 μs | 733.210 μs | 40.190 μs | 187.31 |    1.62 |    7 |      - |  109.2 KB |       16.94 |
 'Lambda Include'                     | 2             |    11.77 μs |   0.661 μs |  0.036 μs |   0.54 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.92 μs |   0.623 μs |  0.034 μs |   0.54 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.79 μs |   2.139 μs |  0.117 μs |   1.13 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    28.06 μs |   0.852 μs |  0.047 μs |   1.28 |    0.00 |    3 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    82.12 μs |   2.770 μs |  0.152 μs |   3.75 |    0.01 |    6 | 0.7324 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.75 μs** |   **1.101 μs** |  **0.060 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.86 μs |   0.563 μs |  0.031 μs |   0.82 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    87.23 μs |   4.252 μs |  0.233 μs |   4.01 |    0.01 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.41 μs |   1.865 μs |  0.102 μs |   1.12 |    0.00 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    51.83 μs |   1.838 μs |  0.101 μs |   2.38 |    0.01 |    5 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.18 μs |   5.629 μs |  0.309 μs |   4.01 |    0.02 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    39.21 μs |   1.795 μs |  0.098 μs |   1.80 |    0.01 |    4 | 0.4272 |  10.74 KB |        1.67 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,074.74 μs | 170.198 μs |  9.329 μs | 187.32 |    0.58 |    7 |      - | 109.35 KB |       16.97 |
 'Lambda Include'                     | 10            |    11.81 μs |   0.208 μs |  0.011 μs |   0.54 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    12.89 μs |  32.360 μs |  1.774 μs |   0.59 |    0.07 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.54 μs |   0.791 μs |  0.043 μs |   1.13 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    28.37 μs |   2.490 μs |  0.136 μs |   1.30 |    0.01 |    3 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    81.80 μs |   3.695 μs |  0.203 μs |   3.76 |    0.01 |    6 | 0.7324 |  19.88 KB |        3.08 |
