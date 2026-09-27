
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.84 μs** |   **5.354 μs** |  **0.293 μs** |   **1.00** |    **0.02** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.31 μs |   2.184 μs |  0.120 μs |   0.79 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.17 μs |   1.337 μs |  0.073 μs |   1.06 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.50 μs |   2.103 μs |  0.115 μs |   1.08 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.94 μs |   2.836 μs |  0.155 μs |   2.29 |    0.03 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    90.24 μs |   4.630 μs |  0.254 μs |   4.13 |    0.05 |    7 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    41.02 μs |   2.542 μs |  0.139 μs |   1.88 |    0.02 |    5 | 0.6104 |  10.73 KB |        1.66 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,925.29 μs | 462.490 μs | 25.351 μs | 179.77 |    2.31 |    8 |      - | 108.83 KB |       16.89 |
 'Lambda Include'                     | 2             |    11.25 μs |   0.819 μs |  0.045 μs |   0.52 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.50 μs |   1.091 μs |  0.060 μs |   0.53 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.57 μs |   1.567 μs |  0.086 μs |   1.13 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.05 μs |   1.268 μs |  0.069 μs |   1.38 |    0.02 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    88.87 μs |  11.419 μs |  0.626 μs |   4.07 |    0.05 |    7 | 1.2207 |  20.04 KB |        3.11 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.82 μs** |   **1.989 μs** |  **0.109 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.12 μs |   1.631 μs |  0.089 μs |   0.79 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.51 μs |   4.984 μs |  0.273 μs |   3.88 |    0.02 |    6 | 1.2207 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    23.73 μs |   1.509 μs |  0.083 μs |   1.04 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    52.63 μs |   3.431 μs |  0.188 μs |   2.31 |    0.01 |    5 | 0.6714 |   11.7 KB |        1.82 |
 'Ten criteria (AND)'                 | 10            |    86.55 μs |   8.784 μs |  0.481 μs |   3.79 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.50 μs |   4.681 μs |  0.257 μs |   1.77 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,972.52 μs | 553.470 μs | 30.338 μs | 174.09 |    1.36 |    7 |      - | 108.99 KB |       16.91 |
 'Lambda Include'                     | 10            |    11.03 μs |   0.529 μs |  0.029 μs |   0.48 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.16 μs |   0.471 μs |  0.026 μs |   0.49 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.52 μs |   0.807 μs |  0.044 μs |   1.12 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.88 μs |   1.515 μs |  0.083 μs |   1.31 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    88.69 μs |   7.643 μs |  0.419 μs |   3.89 |    0.02 |    6 | 1.2207 |   20.2 KB |        3.13 |
