
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                               | CriteriaCount | Mean        | Error     | StdDev    | Median      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|----------:|----------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.34 μs** |  **0.363 μs** |  **0.497 μs** |    **21.33 μs** |   **1.00** |    **0.03** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.18 μs |  0.110 μs |  0.151 μs |    18.24 μs |   0.85 |    0.02 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.37 μs |  0.055 μs |  0.081 μs |    23.35 μs |   1.10 |    0.03 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.66 μs |  0.258 μs |  0.370 μs |    23.83 μs |   1.11 |    0.03 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.06 μs |  0.712 μs |  1.022 μs |    48.37 μs |   2.30 |    0.07 |    8 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    86.90 μs |  0.363 μs |  0.521 μs |    86.88 μs |   4.07 |    0.10 |    9 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.41 μs |  0.107 μs |  0.149 μs |    40.41 μs |   1.89 |    0.04 |    7 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,945.58 μs | 16.176 μs | 24.211 μs | 3,945.99 μs | 184.99 |    4.38 |   10 |      - | 109.76 KB |       17.03 |
 'Lambda Include'                     | 2             |    11.19 μs |  0.049 μs |  0.070 μs |    11.21 μs |   0.52 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.06 μs |  0.218 μs |  0.320 μs |    10.78 μs |   0.52 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.49 μs |  0.535 μs |  0.768 μs |    25.50 μs |   1.20 |    0.04 |    5 | 0.5493 |   9.09 KB |        1.41 |
 'Offset pagination (Skip/Take)'      | 2             |    29.08 μs |  0.169 μs |  0.248 μs |    29.08 μs |   1.36 |    0.03 |    6 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    86.64 μs |  0.238 μs |  0.341 μs |    86.71 μs |   4.06 |    0.09 |    9 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |           |           |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.17 μs** |  **0.104 μs** |  **0.156 μs** |    **22.16 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.80 μs |  0.118 μs |  0.169 μs |    17.81 μs |   0.80 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    86.82 μs |  0.516 μs |  0.724 μs |    86.76 μs |   3.92 |    0.04 |    8 | 1.2207 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    24.39 μs |  0.349 μs |  0.490 μs |    24.77 μs |   1.10 |    0.02 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    48.49 μs |  0.112 μs |  0.164 μs |    48.45 μs |   2.19 |    0.02 |    7 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    85.90 μs |  0.107 μs |  0.151 μs |    85.89 μs |   3.88 |    0.03 |    8 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    39.08 μs |  0.425 μs |  0.582 μs |    39.08 μs |   1.76 |    0.03 |    6 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,934.75 μs | 31.048 μs | 45.509 μs | 3,924.71 μs | 177.51 |    2.36 |    9 | 3.9063 | 109.14 KB |       16.93 |
 'Lambda Include'                     | 10            |    10.80 μs |  0.024 μs |  0.033 μs |    10.79 μs |   0.49 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.19 μs |  0.196 μs |  0.293 μs |    11.17 μs |   0.50 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    23.93 μs |  0.251 μs |  0.351 μs |    24.16 μs |   1.08 |    0.02 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.47 μs |  0.094 μs |  0.140 μs |    29.46 μs |   1.33 |    0.01 |    5 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    86.67 μs |  0.241 μs |  0.361 μs |    86.65 μs |   3.91 |    0.03 |    8 | 1.0986 |  19.88 KB |        3.08 |
