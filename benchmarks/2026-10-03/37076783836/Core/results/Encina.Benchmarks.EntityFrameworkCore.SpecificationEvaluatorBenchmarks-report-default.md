
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.37 μs** |     **3.007 μs** |  **0.165 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.27 μs |     1.121 μs |  0.061 μs |   0.85 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    25.75 μs |     1.646 μs |  0.090 μs |   1.20 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.83 μs |     2.810 μs |  0.154 μs |   1.12 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    51.38 μs |     5.104 μs |  0.280 μs |   2.40 |    0.02 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.40 μs |    12.199 μs |  0.669 μs |   4.14 |    0.04 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    39.99 μs |     3.236 μs |  0.177 μs |   1.87 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,088.14 μs | 1,507.558 μs | 82.634 μs | 191.31 |    3.58 |    6 |      - | 109.39 KB |       16.97 |
 'Lambda Include'                     | 2             |    11.69 μs |     0.509 μs |  0.028 μs |   0.55 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.23 μs |     0.622 μs |  0.034 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.77 μs |     1.210 μs |  0.066 μs |   1.16 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.46 μs |     2.877 μs |  0.158 μs |   1.43 |    0.01 |    2 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    89.41 μs |    11.445 μs |  0.627 μs |   4.18 |    0.04 |    5 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |             |              |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.45 μs** |     **1.840 μs** |  **0.101 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.72 μs |     1.519 μs |  0.083 μs |   0.83 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.53 μs |     6.813 μs |  0.373 μs |   4.13 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.14 μs |     0.504 μs |  0.028 μs |   1.13 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    51.86 μs |     2.979 μs |  0.163 μs |   2.42 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.25 μs |     4.273 μs |  0.234 μs |   4.11 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.13 μs |     5.349 μs |  0.293 μs |   1.87 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,066.11 μs | 1,197.147 μs | 65.620 μs | 189.57 |    2.76 |    8 |      - |  110.2 KB |       17.10 |
 'Lambda Include'                     | 10            |    11.76 μs |     0.797 μs |  0.044 μs |   0.55 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.52 μs |     0.333 μs |  0.018 μs |   0.54 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.62 μs |     0.682 μs |  0.037 μs |   1.15 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    31.90 μs |     4.400 μs |  0.241 μs |   1.49 |    0.01 |    4 | 0.5493 |   9.67 KB |        1.50 |
 'Full specification (all features)'  | 10            |    89.43 μs |     8.383 μs |  0.459 μs |   4.17 |    0.03 |    7 | 1.2207 |  20.03 KB |        3.11 |
