
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.97 μs** |   **1.590 μs** |  **0.087 μs** |   **1.00** |    **0.00** |    **3** | **0.3967** |   **6.73 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.67 μs |   2.015 μs |  0.110 μs |   0.77 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.78 |
 'Complex predicates (parameterized)' | 2             |    24.80 μs |   1.112 μs |  0.061 μs |   1.08 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Two criteria (AND)'                 | 2             |    23.77 μs |   1.727 μs |  0.095 μs |   1.03 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Five criteria (AND)'                | 2             |    48.91 μs |   4.604 μs |  0.252 μs |   2.13 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.72 |
 'Ten criteria (AND)'                 | 2             |    84.60 μs |   2.582 μs |  0.142 μs |   3.68 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.11 |
 'Keyset pagination'                  | 2             |    41.07 μs |   4.146 μs |  0.227 μs |   1.79 |    0.01 |    5 | 0.6104 |  10.73 KB |        1.59 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,900.38 μs | 146.249 μs |  8.016 μs | 169.81 |    0.63 |    7 | 3.9063 | 109.41 KB |       16.27 |
 'Lambda Include'                     | 2             |    11.10 μs |   0.721 μs |  0.039 μs |   0.48 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'String Include'                     | 2             |    11.14 μs |   0.962 μs |  0.053 μs |   0.49 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 2             |    24.59 μs |   1.015 μs |  0.056 μs |   1.07 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 2             |    30.00 μs |   1.385 μs |  0.076 μs |   1.31 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.39 |
 'Full specification (all features)'  | 2             |    90.51 μs |   6.093 μs |  0.334 μs |   3.94 |    0.02 |    6 | 1.2207 |   20.2 KB |        3.00 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **5**             |    **21.75 μs** |   **0.940 μs** |  **0.052 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 5             |    18.06 μs |   1.478 μs |  0.081 μs |   0.83 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 5             |    48.94 μs |   2.775 μs |  0.152 μs |   2.25 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Two criteria (AND)'                 | 5             |    24.11 μs |   2.620 μs |  0.144 μs |   1.11 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 5             |    49.73 μs |   2.408 μs |  0.132 μs |   2.29 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 5             |    86.93 μs |   2.246 μs |  0.123 μs |   4.00 |    0.01 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 5             |    40.40 μs |   2.779 μs |  0.152 μs |   1.86 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 5             | 3,901.32 μs | 340.120 μs | 18.643 μs | 179.40 |    0.83 |    8 | 3.9063 | 110.05 KB |       17.07 |
 'Lambda Include'                     | 5             |    11.54 μs |   0.215 μs |  0.012 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 5             |    11.22 μs |   0.487 μs |  0.027 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 5             |    24.75 μs |   2.923 μs |  0.160 μs |   1.14 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 5             |    29.87 μs |   1.545 μs |  0.085 μs |   1.37 |    0.00 |    4 | 0.5798 |   9.67 KB |        1.50 |
 'Full specification (all features)'  | 5             |    87.72 μs |   4.300 μs |  0.236 μs |   4.03 |    0.01 |    7 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.37 μs** |   **2.404 μs** |  **0.132 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.08 μs |   0.971 μs |  0.053 μs |   0.81 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    85.96 μs |  10.048 μs |  0.551 μs |   3.84 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.08 μs |   1.894 μs |  0.104 μs |   1.08 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.84 μs |   1.830 μs |  0.100 μs |   2.23 |    0.01 |    5 | 0.6714 |   11.7 KB |        1.82 |
 'Ten criteria (AND)'                 | 10            |    88.47 μs |  49.183 μs |  2.696 μs |   3.95 |    0.11 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.77 μs |   1.854 μs |  0.102 μs |   1.82 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,885.35 μs | 654.792 μs | 35.891 μs | 173.67 |    1.65 |    7 |      - | 108.97 KB |       16.91 |
 'Lambda Include'                     | 10            |    11.20 μs |   0.604 μs |  0.033 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.04 μs |   0.498 μs |  0.027 μs |   0.49 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.59 μs |   0.314 μs |  0.017 μs |   1.10 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.19 μs |   3.311 μs |  0.181 μs |   1.30 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    89.10 μs |   3.439 μs |  0.189 μs |   3.98 |    0.02 |    6 | 1.2207 |  20.18 KB |        3.13 |
