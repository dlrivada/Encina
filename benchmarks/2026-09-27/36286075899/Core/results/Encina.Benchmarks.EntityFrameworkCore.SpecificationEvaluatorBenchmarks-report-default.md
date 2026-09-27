
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                               | CriteriaCount | Mean        | Error     | StdDev    | Median      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|----------:|----------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.88 μs** |  **0.340 μs** |  **0.499 μs** |    **22.21 μs** |   **1.00** |    **0.03** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.24 μs |  0.151 μs |  0.222 μs |    18.32 μs |   0.83 |    0.02 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.80 μs |  0.431 μs |  0.631 μs |    24.41 μs |   1.13 |    0.04 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.90 μs |  0.088 μs |  0.129 μs |    24.89 μs |   1.14 |    0.03 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.46 μs |  0.251 μs |  0.360 μs |    50.54 μs |   2.31 |    0.05 |    7 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    89.53 μs |  1.469 μs |  2.153 μs |    88.08 μs |   4.09 |    0.13 |    8 | 1.2207 |  21.37 KB |        3.32 |
 'Keyset pagination'                  | 2             |    40.44 μs |  0.553 μs |  0.794 μs |    40.08 μs |   1.85 |    0.05 |    6 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,939.34 μs | 14.030 μs | 21.000 μs | 3,939.65 μs | 180.10 |    4.16 |    9 |      - |  109.3 KB |       16.96 |
 'Lambda Include'                     | 2             |    11.29 μs |  0.156 μs |  0.229 μs |    11.44 μs |   0.52 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.31 μs |  0.059 μs |  0.085 μs |    11.31 μs |   0.52 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.46 μs |  0.256 μs |  0.367 μs |    25.23 μs |   1.16 |    0.03 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.71 μs |  0.105 μs |  0.157 μs |    30.73 μs |   1.40 |    0.03 |    5 | 0.5493 |   9.67 KB |        1.50 |
 'Full specification (all features)'  | 2             |    87.09 μs |  0.250 μs |  0.367 μs |    87.05 μs |   3.98 |    0.09 |    8 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |           |           |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.19 μs** |  **0.504 μs** |  **0.723 μs** |    **22.17 μs** |   **1.00** |    **0.05** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.55 μs |  0.141 μs |  0.197 μs |    17.68 μs |   0.79 |    0.03 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    87.33 μs |  1.023 μs |  1.400 μs |    86.47 μs |   3.94 |    0.14 |    8 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.35 μs |  0.094 μs |  0.136 μs |    23.30 μs |   1.05 |    0.03 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.07 μs |  0.116 μs |  0.169 μs |    50.04 μs |   2.26 |    0.07 |    7 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    89.53 μs |  0.166 μs |  0.232 μs |    89.50 μs |   4.04 |    0.13 |    8 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 10            |    39.99 μs |  0.421 μs |  0.603 μs |    39.99 μs |   1.80 |    0.06 |    6 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,968.17 μs | 23.189 μs | 34.708 μs | 3,963.50 μs | 179.02 |    5.93 |    9 |      - | 109.02 KB |       16.91 |
 'Lambda Include'                     | 10            |    11.45 μs |  0.170 μs |  0.249 μs |    11.25 μs |   0.52 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.22 μs |  0.114 μs |  0.167 μs |    11.10 μs |   0.51 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.83 μs |  0.192 μs |  0.270 μs |    25.94 μs |   1.17 |    0.04 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.14 μs |  0.172 μs |  0.252 μs |    30.28 μs |   1.36 |    0.04 |    5 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    87.78 μs |  0.239 μs |  0.358 μs |    87.72 μs |   3.96 |    0.13 |    8 | 1.2207 |  20.04 KB |        3.11 |
