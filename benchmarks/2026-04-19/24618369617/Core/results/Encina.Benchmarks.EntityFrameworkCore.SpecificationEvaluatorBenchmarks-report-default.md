
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                               | CriteriaCount | Mean        | Error     | StdDev    | Median      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|----------:|----------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.60 μs** |  **0.159 μs** |  **0.233 μs** |    **21.69 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.82 μs |  0.155 μs |  0.222 μs |    17.84 μs |   0.83 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.18 μs |  0.551 μs |  0.808 μs |    24.76 μs |   1.12 |    0.04 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.80 μs |  0.068 μs |  0.100 μs |    23.80 μs |   1.10 |    0.01 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.66 μs |  0.190 μs |  0.266 μs |    49.58 μs |   2.30 |    0.03 |    7 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    85.90 μs |  0.387 μs |  0.579 μs |    86.05 μs |   3.98 |    0.05 |    8 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.65 μs |  0.692 μs |  0.970 μs |    41.45 μs |   1.88 |    0.05 |    6 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,958.29 μs | 16.762 μs | 25.088 μs | 3,959.44 μs | 183.23 |    2.25 |    9 | 3.9063 | 109.66 KB |       17.01 |
 'Lambda Include'                     | 2             |    11.33 μs |  0.105 μs |  0.154 μs |    11.41 μs |   0.52 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    10.78 μs |  0.209 μs |  0.307 μs |    11.01 μs |   0.50 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.32 μs |  0.046 μs |  0.068 μs |    25.32 μs |   1.17 |    0.01 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    29.90 μs |  0.053 μs |  0.072 μs |    29.90 μs |   1.38 |    0.01 |    5 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    87.67 μs |  1.311 μs |  1.962 μs |    87.61 μs |   4.06 |    0.10 |    8 | 1.2207 |  20.04 KB |        3.11 |
                                      |               |             |           |           |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.75 μs** |  **0.434 μs** |  **0.623 μs** |    **22.34 μs** |   **1.00** |    **0.04** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.37 μs |  0.279 μs |  0.382 μs |    18.39 μs |   0.81 |    0.03 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    86.26 μs |  0.160 μs |  0.220 μs |    86.20 μs |   3.79 |    0.10 |    7 | 1.2207 |  21.06 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    23.63 μs |  0.600 μs |  0.879 μs |    24.31 μs |   1.04 |    0.05 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.72 μs |  0.527 μs |  0.789 μs |    49.68 μs |   2.19 |    0.07 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    85.41 μs |  0.322 μs |  0.462 μs |    85.41 μs |   3.76 |    0.10 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.50 μs |  0.125 μs |  0.179 μs |    40.49 μs |   1.78 |    0.05 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,944.46 μs | 16.446 μs | 24.107 μs | 3,939.53 μs | 173.48 |    4.77 |    8 | 3.9063 |  108.9 KB |       16.90 |
 'Lambda Include'                     | 10            |    11.04 μs |  0.116 μs |  0.166 μs |    11.04 μs |   0.49 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.23 μs |  0.036 μs |  0.052 μs |    11.24 μs |   0.49 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.85 μs |  0.070 μs |  0.094 μs |    24.83 μs |   1.09 |    0.03 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.67 μs |  0.298 μs |  0.437 μs |    29.48 μs |   1.31 |    0.04 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    88.42 μs |  1.744 μs |  2.387 μs |    88.38 μs |   3.89 |    0.15 |    7 | 1.0986 |  19.88 KB |        3.08 |
