
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.00 μs** |     **2.519 μs** |  **0.138 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.46 μs |     1.306 μs |  0.072 μs |   0.84 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.35 μs |     7.938 μs |  0.435 μs |   1.11 |    0.02 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.70 μs |     0.913 μs |  0.050 μs |   1.08 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.54 μs |     2.233 μs |  0.122 μs |   2.25 |    0.01 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    87.28 μs |     6.416 μs |  0.352 μs |   3.97 |    0.03 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.94 μs |     5.581 μs |  0.306 μs |   1.86 |    0.02 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,928.03 μs |   438.106 μs | 24.014 μs | 178.54 |    1.36 |    6 |      - | 110.35 KB |       17.12 |
 'Lambda Include'                     | 2             |    11.19 μs |     0.947 μs |  0.052 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.60 μs |     0.433 μs |  0.024 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.19 μs |     1.090 μs |  0.060 μs |   1.14 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.08 μs |     2.029 μs |  0.111 μs |   1.37 |    0.01 |    2 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    86.97 μs |     7.826 μs |  0.429 μs |   3.95 |    0.03 |    5 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |             |              |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **23.07 μs** |     **1.863 μs** |  **0.102 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.60 μs |     1.272 μs |  0.070 μs |   0.81 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    87.75 μs |    17.925 μs |  0.983 μs |   3.80 |    0.04 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.98 μs |     1.085 μs |  0.059 μs |   1.04 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.20 μs |     8.182 μs |  0.448 μs |   2.13 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.35 μs |     5.717 μs |  0.313 μs |   3.79 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    38.91 μs |     3.036 μs |  0.166 μs |   1.69 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,975.71 μs | 1,198.717 μs | 65.706 μs | 172.37 |    2.55 |    7 |      - | 109.02 KB |       16.91 |
 'Lambda Include'                     | 10            |    10.99 μs |     0.767 μs |  0.042 μs |   0.48 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.33 μs |     0.422 μs |  0.023 μs |   0.49 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.05 μs |     0.960 μs |  0.053 μs |   1.09 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.81 μs |     2.084 μs |  0.114 μs |   1.29 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    90.77 μs |     8.469 μs |  0.464 μs |   3.94 |    0.02 |    6 | 1.2207 |   20.2 KB |        3.13 |
