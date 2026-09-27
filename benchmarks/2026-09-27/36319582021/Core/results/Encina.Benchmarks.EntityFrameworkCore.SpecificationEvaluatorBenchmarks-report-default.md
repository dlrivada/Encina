
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.20 μs** |   **1.539 μs** |  **0.084 μs** |   **1.00** |    **0.00** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.42 μs |   2.593 μs |  0.142 μs |   0.87 |    0.01 |    2 | 0.3357 |   5.54 KB |        0.86 |
 'Complex predicates (parameterized)' | 2             |    23.07 μs |   4.202 μs |  0.230 μs |   1.09 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.24 μs |   1.545 μs |  0.085 μs |   1.10 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    48.85 μs |   4.230 μs |  0.232 μs |   2.30 |    0.01 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    90.55 μs |   5.763 μs |  0.316 μs |   4.27 |    0.02 |    5 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    40.58 μs |   1.599 μs |  0.088 μs |   1.91 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,936.91 μs | 107.308 μs |  5.882 μs | 185.70 |    0.68 |    6 | 3.9063 | 109.49 KB |       16.99 |
 'Lambda Include'                     | 2             |    11.34 μs |   0.516 μs |  0.028 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.01 μs |   1.707 μs |  0.094 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.98 μs |   1.416 μs |  0.078 μs |   1.23 |    0.01 |    2 | 0.5493 |   9.09 KB |        1.41 |
 'Offset pagination (Skip/Take)'      | 2             |    30.02 μs |   1.052 μs |  0.058 μs |   1.42 |    0.01 |    2 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    84.66 μs |   7.083 μs |  0.388 μs |   3.99 |    0.02 |    5 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.00 μs** |   **2.503 μs** |  **0.137 μs** |   **1.00** |    **0.01** |    **3** | **0.3967** |   **6.74 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.58 μs |   1.990 μs |  0.109 μs |   0.80 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.78 |
 'Complex predicates (parameterized)' | 10            |    87.36 μs |   4.849 μs |  0.266 μs |   3.97 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.10 |
 'Two criteria (AND)'                 | 10            |    24.45 μs |   1.134 μs |  0.062 μs |   1.11 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Five criteria (AND)'                | 10            |    49.36 μs |   3.672 μs |  0.201 μs |   2.24 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.71 |
 'Ten criteria (AND)'                 | 10            |    89.87 μs |  31.021 μs |  1.700 μs |   4.09 |    0.07 |    7 | 1.2207 |  20.91 KB |        3.10 |
 'Keyset pagination'                  | 10            |    40.05 μs |   2.483 μs |  0.136 μs |   1.82 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.55 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,932.49 μs | 183.922 μs | 10.081 μs | 178.75 |    1.04 |    8 | 3.9063 | 108.89 KB |       16.15 |
 'Lambda Include'                     | 10            |    11.07 μs |   0.510 μs |  0.028 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'String Include'                     | 10            |    11.33 μs |   0.508 μs |  0.028 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 10            |    24.81 μs |   3.339 μs |  0.183 μs |   1.13 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 10            |    31.05 μs |   4.861 μs |  0.266 μs |   1.41 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.39 |
 'Full specification (all features)'  | 10            |    86.50 μs |   6.981 μs |  0.383 μs |   3.93 |    0.03 |    7 | 1.0986 |  19.88 KB |        2.95 |
