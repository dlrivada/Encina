
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **20.49 μs** |   **0.575 μs** |  **0.032 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    16.88 μs |   0.725 μs |  0.040 μs |   0.82 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.64 μs |   2.690 μs |  0.147 μs |   1.15 |    0.01 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.47 μs |   1.345 μs |  0.074 μs |   1.19 |    0.00 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    51.10 μs |   6.461 μs |  0.354 μs |   2.49 |    0.02 |    5 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    87.37 μs |   4.509 μs |  0.247 μs |   4.26 |    0.01 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    38.18 μs |   3.341 μs |  0.183 μs |   1.86 |    0.01 |    4 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,887.10 μs | 202.762 μs | 11.114 μs | 189.70 |    0.53 |    7 | 3.9063 | 108.95 KB |       16.90 |
 'Lambda Include'                     | 2             |    11.51 μs |   0.526 μs |  0.029 μs |   0.56 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.53 μs |   0.962 μs |  0.053 μs |   0.56 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.92 μs |   0.569 μs |  0.031 μs |   1.22 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    27.66 μs |   1.463 μs |  0.080 μs |   1.35 |    0.00 |    3 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    82.19 μs |   3.739 μs |  0.205 μs |   4.01 |    0.01 |    6 | 0.7324 |   20.2 KB |        3.13 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **20.27 μs** |   **0.407 μs** |  **0.022 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    16.66 μs |   1.518 μs |  0.083 μs |   0.82 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    87.41 μs |   4.456 μs |  0.244 μs |   4.31 |    0.01 |    6 | 0.8545 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    24.41 μs |   1.801 μs |  0.099 μs |   1.20 |    0.00 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    51.40 μs |   2.412 μs |  0.132 μs |   2.54 |    0.01 |    5 | 0.4272 |  11.85 KB |        1.84 |
 'Ten criteria (AND)'                 | 10            |    88.31 μs |   6.466 μs |  0.354 μs |   4.36 |    0.02 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    38.23 μs |   2.887 μs |  0.158 μs |   1.89 |    0.01 |    4 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,897.60 μs | 433.531 μs | 23.763 μs | 192.27 |    1.03 |    7 | 3.9063 | 108.78 KB |       16.88 |
 'Lambda Include'                     | 10            |    11.57 μs |   0.515 μs |  0.028 μs |   0.57 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.62 μs |   0.600 μs |  0.033 μs |   0.57 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.25 μs |   1.158 μs |  0.063 μs |   1.20 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    27.37 μs |   0.636 μs |  0.035 μs |   1.35 |    0.00 |    3 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    81.56 μs |   5.775 μs |  0.317 μs |   4.02 |    0.01 |    6 | 0.7324 |  20.04 KB |        3.11 |
