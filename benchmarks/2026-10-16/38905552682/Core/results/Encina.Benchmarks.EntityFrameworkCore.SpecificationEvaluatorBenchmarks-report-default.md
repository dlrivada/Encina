
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.24 μs** |   **1.617 μs** |  **0.089 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.85 μs |   1.426 μs |  0.078 μs |   0.80 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.69 μs |   1.880 μs |  0.103 μs |   1.07 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.73 μs |   0.542 μs |  0.030 μs |   1.07 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.00 μs |   2.093 μs |  0.115 μs |   2.20 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.80 μs |  14.314 μs |  0.785 μs |   3.99 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    39.70 μs |   4.433 μs |  0.243 μs |   1.78 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,959.02 μs | 702.317 μs | 38.496 μs | 178.00 |    1.62 |    7 |      - | 109.25 KB |       16.95 |
 'Lambda Include'                     | 2             |    11.25 μs |   0.664 μs |  0.036 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.50 μs |   0.451 μs |  0.025 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.98 μs |   1.151 μs |  0.063 μs |   1.12 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    29.76 μs |   0.998 μs |  0.055 μs |   1.34 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    87.90 μs |   6.688 μs |  0.367 μs |   3.95 |    0.02 |    6 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.64 μs** |   **2.766 μs** |  **0.152 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.67 μs |   1.545 μs |  0.085 μs |   0.82 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    87.60 μs |   4.518 μs |  0.248 μs |   3.87 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.69 μs |   1.300 μs |  0.071 μs |   1.05 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.94 μs |   2.476 μs |  0.136 μs |   2.25 |    0.01 |    5 | 0.6714 |   11.7 KB |        1.82 |
 'Ten criteria (AND)'                 | 10            |    87.97 μs |   3.461 μs |  0.190 μs |   3.89 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    39.95 μs |   3.314 μs |  0.182 μs |   1.76 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,974.67 μs | 416.451 μs | 22.827 μs | 175.56 |    1.34 |    7 | 3.9063 | 109.62 KB |       17.01 |
 'Lambda Include'                     | 10            |    11.50 μs |   0.885 μs |  0.049 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.24 μs |   0.540 μs |  0.030 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.66 μs |   1.074 μs |  0.059 μs |   1.13 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.33 μs |   8.697 μs |  0.477 μs |   1.34 |    0.02 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    85.83 μs |  14.786 μs |  0.810 μs |   3.79 |    0.04 |    6 | 1.0986 |  19.88 KB |        3.08 |
