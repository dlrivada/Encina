
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.53 μs** |     **2.830 μs** |  **0.155 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.01 μs |     1.654 μs |  0.091 μs |   0.80 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.61 μs |     1.726 μs |  0.095 μs |   1.05 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.15 μs |     2.373 μs |  0.130 μs |   1.07 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.80 μs |     4.543 μs |  0.249 μs |   2.25 |    0.02 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    89.38 μs |     8.045 μs |  0.441 μs |   3.97 |    0.03 |    7 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    39.29 μs |     3.416 μs |  0.187 μs |   1.74 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,126.11 μs | 1,187.104 μs | 65.069 μs | 183.11 |    2.73 |    8 |      - | 109.42 KB |       16.98 |
 'Lambda Include'                     | 2             |    11.34 μs |     1.058 μs |  0.058 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.27 μs |     2.169 μs |  0.119 μs |   0.50 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.30 μs |     1.566 μs |  0.086 μs |   1.12 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.74 μs |     3.105 μs |  0.170 μs |   1.36 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    88.19 μs |    12.762 μs |  0.700 μs |   3.91 |    0.04 |    7 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |              |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.03 μs** |     **1.581 μs** |  **0.087 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.36 μs |     1.638 μs |  0.090 μs |   0.79 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    91.17 μs |     8.087 μs |  0.443 μs |   4.14 |    0.02 |    7 | 1.2207 |  21.22 KB |        3.29 |
 'Two criteria (AND)'                 | 10            |    23.99 μs |     3.644 μs |  0.200 μs |   1.09 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.25 μs |     5.066 μs |  0.278 μs |   2.28 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.11 μs |    13.018 μs |  0.714 μs |   4.00 |    0.03 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.12 μs |     2.937 μs |  0.161 μs |   1.82 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,100.04 μs |   820.052 μs | 44.950 μs | 186.15 |    1.88 |    8 |      - | 108.75 KB |       16.87 |
 'Lambda Include'                     | 10            |    11.66 μs |     0.761 μs |  0.042 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.49 μs |     0.879 μs |  0.048 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.66 μs |     2.480 μs |  0.136 μs |   1.16 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    31.58 μs |     3.459 μs |  0.190 μs |   1.43 |    0.01 |    4 | 0.5493 |   9.66 KB |        1.50 |
 'Full specification (all features)'  | 10            |    87.42 μs |     8.837 μs |  0.484 μs |   3.97 |    0.02 |    7 | 1.0986 |  19.88 KB |        3.08 |
