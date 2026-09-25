
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **23.95 μs** |   **1.349 μs** |  **0.074 μs** |   **1.00** |    **0.00** |    **3** | **0.3967** |   **6.73 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.04 μs |   1.130 μs |  0.062 μs |   0.71 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.78 |
 'Complex predicates (parameterized)' | 2             |    23.64 μs |   1.175 μs |  0.064 μs |   0.99 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Two criteria (AND)'                 | 2             |    24.83 μs |   0.881 μs |  0.048 μs |   1.04 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Five criteria (AND)'                | 2             |    50.28 μs |   2.666 μs |  0.146 μs |   2.10 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.72 |
 'Ten criteria (AND)'                 | 2             |    90.84 μs |  13.805 μs |  0.757 μs |   3.79 |    0.03 |    7 | 1.2207 |  20.91 KB |        3.11 |
 'Keyset pagination'                  | 2             |    38.70 μs |   6.112 μs |  0.335 μs |   1.62 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.55 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,952.55 μs | 244.831 μs | 13.420 μs | 165.01 |    0.66 |    8 |      - | 109.85 KB |       16.33 |
 'Lambda Include'                     | 2             |    11.31 μs |   1.044 μs |  0.057 μs |   0.47 |    0.00 |    1 | 0.2441 |   4.26 KB |        0.63 |
 'String Include'                     | 2             |    11.48 μs |   0.372 μs |  0.020 μs |   0.48 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 2             |    24.92 μs |   0.300 μs |  0.016 μs |   1.04 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 2             |    30.96 μs |  10.066 μs |  0.552 μs |   1.29 |    0.02 |    4 | 0.5493 |   9.36 KB |        1.39 |
 'Full specification (all features)'  | 2             |    87.89 μs |   6.640 μs |  0.364 μs |   3.67 |    0.02 |    7 | 1.0986 |  19.88 KB |        2.96 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.14 μs** |   **0.928 μs** |  **0.051 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.24 μs |   1.204 μs |  0.066 μs |   0.82 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    86.51 μs |  14.043 μs |  0.770 μs |   4.09 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.87 μs |   2.146 μs |  0.118 μs |   1.13 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.93 μs |   2.560 μs |  0.140 μs |   2.41 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.47 μs |   2.372 μs |  0.130 μs |   4.14 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    41.83 μs |   9.377 μs |  0.514 μs |   1.98 |    0.02 |    4 | 0.6104 |  10.73 KB |        1.66 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,961.87 μs | 716.922 μs | 39.297 μs | 187.44 |    1.66 |    7 |      - |  109.6 KB |       17.00 |
 'Lambda Include'                     | 10            |    11.38 μs |   1.900 μs |  0.104 μs |   0.54 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.16 μs |   0.726 μs |  0.040 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.14 μs |   1.603 μs |  0.088 μs |   1.19 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.13 μs |   1.396 μs |  0.077 μs |   1.43 |    0.00 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    87.38 μs |   4.680 μs |  0.257 μs |   4.13 |    0.01 |    6 | 1.2207 |  20.03 KB |        3.11 |
