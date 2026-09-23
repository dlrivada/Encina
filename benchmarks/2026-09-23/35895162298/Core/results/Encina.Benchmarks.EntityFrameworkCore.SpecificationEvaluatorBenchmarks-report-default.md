
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.77 μs** |   **3.078 μs** |  **0.169 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.38 μs |   3.796 μs |  0.208 μs |   0.80 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.21 μs |   4.164 μs |  0.228 μs |   1.07 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    22.34 μs |   5.292 μs |  0.290 μs |   1.03 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    48.58 μs |  14.979 μs |  0.821 μs |   2.23 |    0.04 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    85.51 μs |  25.135 μs |  1.378 μs |   3.93 |    0.06 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    38.74 μs |  11.745 μs |  0.644 μs |   1.78 |    0.03 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,789.55 μs | 566.968 μs | 31.077 μs | 174.08 |    1.70 |    7 | 3.9063 | 109.17 KB |       16.94 |
 'Lambda Include'                     | 2             |    11.01 μs |   2.029 μs |  0.111 μs |   0.51 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.33 μs |  11.984 μs |  0.657 μs |   0.52 |    0.03 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.18 μs |   1.050 μs |  0.058 μs |   1.16 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    28.63 μs |   9.002 μs |  0.493 μs |   1.32 |    0.02 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    89.02 μs |   7.661 μs |  0.420 μs |   4.09 |    0.03 |    6 | 1.2207 |   20.2 KB |        3.13 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.06 μs** |   **2.169 μs** |  **0.119 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.94 μs |   2.716 μs |  0.149 μs |   0.86 |    0.01 |    2 | 0.3357 |   5.55 KB |        0.86 |
 'Complex predicates (parameterized)' | 10            |    84.35 μs |  25.477 μs |  1.396 μs |   3.82 |    0.06 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    21.88 μs |   4.118 μs |  0.226 μs |   0.99 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    48.75 μs |  14.608 μs |  0.801 μs |   2.21 |    0.03 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    82.44 μs |  29.221 μs |  1.602 μs |   3.74 |    0.07 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    38.56 μs |  11.038 μs |  0.605 μs |   1.75 |    0.03 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,907.23 μs | 476.874 μs | 26.139 μs | 177.10 |    1.32 |    7 |      - | 109.33 KB |       16.96 |
 'Lambda Include'                     | 10            |    10.66 μs |   2.861 μs |  0.157 μs |   0.48 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    10.81 μs |   2.099 μs |  0.115 μs |   0.49 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.05 μs |   6.246 μs |  0.342 μs |   1.09 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.17 μs |  12.961 μs |  0.710 μs |   1.32 |    0.03 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    84.70 μs |  18.081 μs |  0.991 μs |   3.84 |    0.04 |    6 | 1.2207 |  20.03 KB |        3.11 |
