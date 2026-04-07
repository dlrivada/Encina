
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.76 μs** |   **3.581 μs** |  **0.196 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.77 μs |   1.492 μs |  0.082 μs |   0.82 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.27 μs |   2.632 μs |  0.144 μs |   1.12 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    25.08 μs |   1.320 μs |  0.072 μs |   1.15 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.18 μs |   6.430 μs |  0.352 μs |   2.31 |    0.02 |    5 | 0.6714 |  11.71 KB |        1.82 |
 'Ten criteria (AND)'                 | 2             |    92.15 μs |  36.513 μs |  2.001 μs |   4.24 |    0.09 |    6 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    40.87 μs |   2.993 μs |  0.164 μs |   1.88 |    0.02 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,962.61 μs | 544.024 μs | 29.820 μs | 182.12 |    1.85 |    7 |      - | 109.21 KB |       16.94 |
 'Lambda Include'                     | 2             |    10.88 μs |   1.052 μs |  0.058 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    10.97 μs |   0.291 μs |  0.016 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.38 μs |   0.887 μs |  0.049 μs |   1.17 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.24 μs |   5.849 μs |  0.321 μs |   1.39 |    0.02 |    3 | 0.5798 |   9.66 KB |        1.50 |
 'Full specification (all features)'  | 2             |    89.75 μs |  25.813 μs |  1.415 μs |   4.12 |    0.06 |    6 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **23.25 μs** |   **2.340 μs** |  **0.128 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.55 μs |   1.527 μs |  0.084 μs |   0.75 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    87.54 μs |  27.859 μs |  1.527 μs |   3.76 |    0.06 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.48 μs |   0.215 μs |  0.012 μs |   1.05 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.13 μs |   2.854 μs |  0.156 μs |   2.16 |    0.01 |    6 | 0.6714 |   11.7 KB |        1.82 |
 'Ten criteria (AND)'                 | 10            |    88.93 μs |  11.490 μs |  0.630 μs |   3.82 |    0.03 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    38.85 μs |   1.889 μs |  0.104 μs |   1.67 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,936.64 μs | 766.434 μs | 42.011 μs | 169.30 |    1.76 |    8 |      - | 110.34 KB |       17.12 |
 'Lambda Include'                     | 10            |    10.97 μs |   0.696 μs |  0.038 μs |   0.47 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    10.87 μs |   1.067 μs |  0.058 μs |   0.47 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.28 μs |   1.625 μs |  0.089 μs |   1.04 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.13 μs |   2.234 μs |  0.122 μs |   1.30 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    87.97 μs |   9.757 μs |  0.535 μs |   3.78 |    0.03 |    7 | 1.0986 |  19.88 KB |        3.08 |
