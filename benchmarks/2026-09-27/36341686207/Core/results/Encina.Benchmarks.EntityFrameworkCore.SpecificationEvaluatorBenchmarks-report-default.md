
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.33 μs** |   **0.754 μs** |  **0.041 μs** |   **1.00** |    **0.00** |    **3** | **0.3967** |   **6.55 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    16.74 μs |   1.377 μs |  0.075 μs |   0.75 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.80 |
 'Complex predicates (parameterized)' | 2             |    23.50 μs |   2.333 μs |  0.128 μs |   1.05 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.94 |
 'Two criteria (AND)'                 | 2             |    24.50 μs |   1.162 μs |  0.064 μs |   1.10 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.94 |
 'Five criteria (AND)'                | 2             |    48.65 μs |   3.380 μs |  0.185 μs |   2.18 |    0.01 |    4 | 0.6714 |  11.55 KB |        1.76 |
 'Ten criteria (AND)'                 | 2             |    93.41 μs |  10.246 μs |  0.562 μs |   4.18 |    0.02 |    5 | 1.2207 |  21.23 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.85 μs |   3.788 μs |  0.208 μs |   1.83 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.59 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,929.92 μs | 173.068 μs |  9.486 μs | 176.02 |    0.46 |    6 | 3.9063 |  108.7 KB |       16.58 |
 'Lambda Include'                     | 2             |    11.15 μs |   0.547 μs |  0.030 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.65 |
 'String Include'                     | 2             |    11.42 μs |   1.575 μs |  0.086 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.65 |
 'Multi-column ordering'              | 2             |    25.59 μs |   1.027 μs |  0.056 μs |   1.15 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.34 |
 'Offset pagination (Skip/Take)'      | 2             |    30.47 μs |   2.733 μs |  0.150 μs |   1.36 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.43 |
 'Full specification (all features)'  | 2             |    87.08 μs |   4.459 μs |  0.244 μs |   3.90 |    0.01 |    5 | 1.0986 |  19.88 KB |        3.03 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.84 μs** |   **4.244 μs** |  **0.233 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.72 μs |   1.847 μs |  0.101 μs |   0.81 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    86.24 μs |   5.266 μs |  0.289 μs |   3.95 |    0.04 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.43 μs |   2.533 μs |  0.139 μs |   1.12 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.64 μs |   3.260 μs |  0.179 μs |   2.32 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    92.24 μs |   4.682 μs |  0.257 μs |   4.22 |    0.04 |    6 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 10            |    40.34 μs |   7.214 μs |  0.395 μs |   1.85 |    0.02 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,925.47 μs | 475.628 μs | 26.071 μs | 179.72 |    1.95 |    7 | 3.9063 | 108.86 KB |       16.89 |
 'Lambda Include'                     | 10            |    10.96 μs |   0.832 μs |  0.046 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.34 μs |   0.753 μs |  0.041 μs |   0.52 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.98 μs |   2.202 μs |  0.121 μs |   1.19 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.43 μs |   3.745 μs |  0.205 μs |   1.39 |    0.02 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    91.00 μs |   6.707 μs |  0.368 μs |   4.17 |    0.04 |    6 | 1.2207 |  20.04 KB |        3.11 |
