
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.70 μs** |   **1.820 μs** |  **0.100 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.68 μs |   2.074 μs |  0.114 μs |   0.81 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.63 μs |   3.221 μs |  0.177 μs |   1.09 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.16 μs |   1.858 μs |  0.102 μs |   1.07 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    48.56 μs |   3.596 μs |  0.197 μs |   2.24 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.64 μs |   7.012 μs |  0.384 μs |   4.08 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.97 μs |   2.823 μs |  0.155 μs |   1.89 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,950.58 μs | 205.268 μs | 11.251 μs | 182.05 |    0.85 |    7 |      - | 108.79 KB |       16.88 |
 'Lambda Include'                     | 2             |    11.62 μs |   0.505 μs |  0.028 μs |   0.54 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.73 μs |   0.646 μs |  0.035 μs |   0.54 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.50 μs |   1.197 μs |  0.066 μs |   1.18 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.84 μs |   2.395 μs |  0.131 μs |   1.42 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    90.03 μs |  10.594 μs |  0.581 μs |   4.15 |    0.03 |    6 | 1.2207 |  20.34 KB |        3.16 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.84 μs** |   **2.137 μs** |  **0.117 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.73 μs |   1.509 μs |  0.083 μs |   0.81 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    87.58 μs |   7.579 μs |  0.415 μs |   4.01 |    0.02 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.15 μs |   2.216 μs |  0.121 μs |   1.11 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.25 μs |   4.804 μs |  0.263 μs |   2.25 |    0.01 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.79 μs |   9.974 μs |  0.547 μs |   4.02 |    0.03 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    41.21 μs |   2.434 μs |  0.133 μs |   1.89 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,969.44 μs | 597.724 μs | 32.763 μs | 181.72 |    1.55 |    6 |      - | 109.36 KB |       16.97 |
 'Lambda Include'                     | 10            |    11.64 μs |   0.661 μs |  0.036 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.64 μs |   1.997 μs |  0.109 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.79 μs |   1.657 μs |  0.091 μs |   1.18 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.04 μs |   0.915 μs |  0.050 μs |   1.38 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    86.39 μs |   6.527 μs |  0.358 μs |   3.96 |    0.02 |    5 | 1.0986 |  19.88 KB |        3.08 |
