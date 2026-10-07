
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **20.87 μs** |   **1.282 μs** |  **0.070 μs** |   **1.00** |    **0.00** |    **2** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.76 μs |   0.766 μs |  0.042 μs |   0.85 |    0.00 |    2 | 0.2136 |   5.55 KB |        0.86 |
 'Complex predicates (parameterized)' | 2             |    24.17 μs |   1.788 μs |  0.098 μs |   1.16 |    0.01 |    2 | 0.2441 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.80 μs |   0.548 μs |  0.030 μs |   1.19 |    0.00 |    2 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.46 μs |   2.452 μs |  0.134 μs |   2.42 |    0.01 |    4 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    86.68 μs |   3.429 μs |  0.188 μs |   4.15 |    0.01 |    5 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    38.52 μs |   2.880 μs |  0.158 μs |   1.85 |    0.01 |    3 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,926.65 μs | 630.879 μs | 34.581 μs | 188.15 |    1.54 |    6 |      - | 109.77 KB |       17.03 |
 'Lambda Include'                     | 2             |    11.61 μs |   0.612 μs |  0.034 μs |   0.56 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.75 μs |   0.423 μs |  0.023 μs |   0.56 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.54 μs |   3.097 μs |  0.170 μs |   1.18 |    0.01 |    2 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    26.99 μs |   2.498 μs |  0.137 μs |   1.29 |    0.01 |    2 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    83.98 μs |   6.186 μs |  0.339 μs |   4.02 |    0.02 |    5 | 0.7324 |  20.34 KB |        3.16 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **20.32 μs** |   **0.778 μs** |  **0.043 μs** |   **1.00** |    **0.00** |    **2** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.00 μs |   1.145 μs |  0.063 μs |   0.84 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    85.78 μs |   5.188 μs |  0.284 μs |   4.22 |    0.01 |    5 | 0.7324 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.34 μs |   1.388 μs |  0.076 μs |   1.20 |    0.00 |    2 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.70 μs |   2.804 μs |  0.154 μs |   2.45 |    0.01 |    4 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.07 μs |   1.801 μs |  0.099 μs |   4.29 |    0.01 |    5 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    37.25 μs |   1.065 μs |  0.058 μs |   1.83 |    0.00 |    3 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,940.51 μs | 341.702 μs | 18.730 μs | 193.95 |    0.87 |    6 | 3.9063 | 109.61 KB |       17.01 |
 'Lambda Include'                     | 10            |    11.70 μs |   1.230 μs |  0.067 μs |   0.58 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.55 μs |   0.284 μs |  0.016 μs |   0.57 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.69 μs |   1.031 μs |  0.056 μs |   1.22 |    0.00 |    2 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    27.00 μs |   0.294 μs |  0.016 μs |   1.33 |    0.00 |    2 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    81.65 μs |   3.064 μs |  0.168 μs |   4.02 |    0.01 |    5 | 0.7324 |  20.04 KB |        3.11 |
