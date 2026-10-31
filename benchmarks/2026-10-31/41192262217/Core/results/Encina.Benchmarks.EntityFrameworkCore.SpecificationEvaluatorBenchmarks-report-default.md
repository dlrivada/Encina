
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.41 μs** |   **1.668 μs** |  **0.091 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.56 μs |   0.882 μs |  0.048 μs |   0.83 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.70 μs |   1.343 μs |  0.074 μs |   1.06 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    25.21 μs |   1.834 μs |  0.101 μs |   1.12 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.91 μs |   4.077 μs |  0.223 μs |   2.27 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    89.27 μs |  12.816 μs |  0.702 μs |   3.98 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    41.11 μs |   3.286 μs |  0.180 μs |   1.83 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,971.79 μs | 415.259 μs | 22.762 μs | 177.20 |    1.08 |    7 |      - | 108.92 KB |       16.90 |
 'Lambda Include'                     | 2             |    11.30 μs |   3.943 μs |  0.216 μs |   0.50 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.14 μs |   2.015 μs |  0.110 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.98 μs |   0.656 μs |  0.036 μs |   1.16 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.35 μs |   1.865 μs |  0.102 μs |   1.35 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    90.30 μs |   5.407 μs |  0.296 μs |   4.03 |    0.02 |    6 | 1.2207 |  20.18 KB |        3.13 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.30 μs** |   **1.994 μs** |  **0.109 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.94 μs |   0.912 μs |  0.050 μs |   0.80 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    89.53 μs |   7.588 μs |  0.416 μs |   4.02 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.02 μs |   2.207 μs |  0.121 μs |   1.08 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.38 μs |   2.978 μs |  0.163 μs |   2.26 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.39 μs |   1.852 μs |  0.102 μs |   3.96 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.86 μs |   4.501 μs |  0.247 μs |   1.83 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,955.83 μs | 571.990 μs | 31.353 μs | 177.40 |    1.43 |    7 |      - | 109.32 KB |       16.96 |
 'Lambda Include'                     | 10            |    11.17 μs |   1.704 μs |  0.093 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.39 μs |   1.473 μs |  0.081 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.63 μs |   1.478 μs |  0.081 μs |   1.15 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.57 μs |   5.258 μs |  0.288 μs |   1.37 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    91.02 μs |   7.431 μs |  0.407 μs |   4.08 |    0.02 |    6 | 1.2207 |   20.2 KB |        3.13 |
