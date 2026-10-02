
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.01 μs** |     **2.771 μs** |  **0.152 μs** |   **1.00** |    **0.01** |    **3** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.50 μs |     1.167 μs |  0.064 μs |   0.83 |    0.01 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.23 μs |     2.829 μs |  0.155 μs |   1.15 |    0.01 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.73 μs |     0.557 μs |  0.031 μs |   1.18 |    0.01 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.74 μs |     2.822 μs |  0.155 μs |   2.42 |    0.02 |    5 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    87.48 μs |     5.659 μs |  0.310 μs |   4.16 |    0.03 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    37.70 μs |     1.067 μs |  0.059 μs |   1.79 |    0.01 |    4 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,928.63 μs | 1,004.034 μs | 55.034 μs | 187.02 |    2.56 |    7 | 3.9063 | 109.25 KB |       16.95 |
 'Lambda Include'                     | 2             |    11.57 μs |     0.345 μs |  0.019 μs |   0.55 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.45 μs |     0.295 μs |  0.016 μs |   0.54 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.47 μs |     0.924 μs |  0.051 μs |   1.16 |    0.01 |    3 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    27.43 μs |     0.729 μs |  0.040 μs |   1.31 |    0.01 |    3 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    81.43 μs |     5.053 μs |  0.277 μs |   3.88 |    0.03 |    6 | 0.7324 |  19.88 KB |        3.08 |
                                      |               |             |              |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **20.98 μs** |     **0.203 μs** |  **0.011 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    16.97 μs |     1.217 μs |  0.067 μs |   0.81 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.19 μs |    26.456 μs |  1.450 μs |   4.20 |    0.06 |    6 | 0.8545 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    25.00 μs |     0.824 μs |  0.045 μs |   1.19 |    0.00 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.74 μs |     3.403 μs |  0.187 μs |   2.42 |    0.01 |    5 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.24 μs |     1.267 μs |  0.069 μs |   4.21 |    0.00 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    38.29 μs |     1.757 μs |  0.096 μs |   1.82 |    0.00 |    4 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,938.27 μs |   382.513 μs | 20.967 μs | 187.69 |    0.87 |    7 | 3.9063 | 109.19 KB |       16.94 |
 'Lambda Include'                     | 10            |    11.60 μs |     0.662 μs |  0.036 μs |   0.55 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.57 μs |     0.800 μs |  0.044 μs |   0.55 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.39 μs |     0.257 μs |  0.014 μs |   1.16 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    27.12 μs |     1.873 μs |  0.103 μs |   1.29 |    0.00 |    3 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    81.25 μs |     5.401 μs |  0.296 μs |   3.87 |    0.01 |    6 | 0.7324 |  19.88 KB |        3.08 |
