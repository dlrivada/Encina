
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.66 μs** |   **1.644 μs** |  **0.090 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.16 μs |   2.039 μs |  0.112 μs |   0.76 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    22.99 μs |   2.340 μs |  0.128 μs |   1.01 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    22.86 μs |   1.226 μs |  0.067 μs |   1.01 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.58 μs |   5.625 μs |  0.308 μs |   2.19 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    86.23 μs |   4.295 μs |  0.235 μs |   3.80 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.64 μs |   6.549 μs |  0.359 μs |   1.79 |    0.02 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,980.48 μs | 513.556 μs | 28.150 μs | 175.63 |    1.23 |    7 | 3.9063 | 109.26 KB |       16.95 |
 'Lambda Include'                     | 2             |    11.00 μs |   0.923 μs |  0.051 μs |   0.49 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    10.90 μs |   0.470 μs |  0.026 μs |   0.48 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.56 μs |   1.378 μs |  0.076 μs |   1.08 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    29.35 μs |   2.272 μs |  0.125 μs |   1.30 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    87.37 μs |   3.519 μs |  0.193 μs |   3.86 |    0.02 |    6 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.92 μs** |   **2.390 μs** |  **0.131 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.98 μs |   1.372 μs |  0.075 μs |   0.82 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.79 μs |   6.124 μs |  0.336 μs |   4.05 |    0.02 |    6 | 1.2207 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    24.55 μs |   2.720 μs |  0.149 μs |   1.12 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.61 μs |   1.103 μs |  0.060 μs |   2.26 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    86.24 μs |   7.237 μs |  0.397 μs |   3.94 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.19 μs |   3.234 μs |  0.177 μs |   1.83 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,021.59 μs | 574.466 μs | 31.488 μs | 183.49 |    1.57 |    7 |      - | 109.93 KB |       17.06 |
 'Lambda Include'                     | 10            |    11.28 μs |   1.202 μs |  0.066 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.06 μs |   0.854 μs |  0.047 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.62 μs |   1.051 μs |  0.058 μs |   1.12 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.01 μs |   2.418 μs |  0.133 μs |   1.32 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    88.86 μs |   1.414 μs |  0.078 μs |   4.05 |    0.02 |    6 | 1.0986 |  19.88 KB |        3.08 |
