
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **14.448 μs** |   **0.5577 μs** |  **0.0306 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    12.417 μs |   0.8084 μs |  0.0443 μs |   0.86 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    16.699 μs |   1.4076 μs |  0.0772 μs |   1.16 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    16.696 μs |   1.1292 μs |  0.0619 μs |   1.16 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    38.106 μs |   1.2335 μs |  0.0676 μs |   2.64 |    0.01 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    71.654 μs |   6.3045 μs |  0.3456 μs |   4.96 |    0.02 |    6 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    26.338 μs |   1.8814 μs |  0.1031 μs |   1.82 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,132.014 μs | 576.3082 μs | 31.5894 μs | 285.99 |    1.96 |    7 |      - | 109.08 KB |       16.92 |
 'Lambda Include'                     | 2             |     8.931 μs |   0.1938 μs |  0.0106 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     8.988 μs |   0.9344 μs |  0.0512 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    20.398 μs |   1.2262 μs |  0.0672 μs |   1.41 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    21.978 μs |   2.2922 μs |  0.1256 μs |   1.52 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    57.890 μs |  15.8752 μs |  0.8702 μs |   4.01 |    0.05 |    5 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **14.233 μs** |   **1.2494 μs** |  **0.0685 μs** |   **1.00** |    **0.01** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    12.373 μs |   0.6979 μs |  0.0383 μs |   0.87 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    71.177 μs |   4.4784 μs |  0.2455 μs |   5.00 |    0.03 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    16.638 μs |   1.6504 μs |  0.0905 μs |   1.17 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    37.387 μs |   1.9535 μs |  0.1071 μs |   2.63 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    71.745 μs |   2.1927 μs |  0.1202 μs |   5.04 |    0.02 |    7 | 1.2207 |  21.06 KB |        3.27 |
 'Keyset pagination'                  | 10            |    26.354 μs |   1.8706 μs |  0.1025 μs |   1.85 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,106.314 μs | 497.3406 μs | 27.2609 μs | 288.51 |    2.05 |    8 |      - |  109.4 KB |       16.97 |
 'Lambda Include'                     | 10            |     9.110 μs |   0.3996 μs |  0.0219 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     9.116 μs |   0.2683 μs |  0.0147 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    20.849 μs |   0.5099 μs |  0.0279 μs |   1.46 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    21.877 μs |   1.8195 μs |  0.0997 μs |   1.54 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    58.738 μs |   2.0327 μs |  0.1114 μs |   4.13 |    0.02 |    6 | 1.1597 |  19.88 KB |        3.08 |
