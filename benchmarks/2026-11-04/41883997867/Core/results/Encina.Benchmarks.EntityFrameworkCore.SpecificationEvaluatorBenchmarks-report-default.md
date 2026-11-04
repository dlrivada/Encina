
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.447 μs** |   **0.2828 μs** |  **0.0155 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    10.523 μs |   0.8961 μs |  0.0491 μs |   0.92 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    13.716 μs |   2.1765 μs |  0.1193 μs |   1.20 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.370 μs |   1.6768 μs |  0.0919 μs |   1.17 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.638 μs |   2.4524 μs |  0.1344 μs |   2.59 |    0.01 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    56.403 μs |   1.5162 μs |  0.0831 μs |   4.93 |    0.01 |    5 | 1.2817 |  21.37 KB |        3.32 |
 'Keyset pagination'                  | 2             |    20.513 μs |   1.3516 μs |  0.0741 μs |   1.79 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,461.052 μs | 998.3806 μs | 54.7246 μs | 302.36 |    4.16 |    6 | 3.9063 | 110.29 KB |       17.11 |
 'Lambda Include'                     | 2             |     7.393 μs |   1.2625 μs |  0.0692 μs |   0.65 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.238 μs |   0.0514 μs |  0.0028 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    16.538 μs |   0.9597 μs |  0.0526 μs |   1.44 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    17.276 μs |   0.4183 μs |  0.0229 μs |   1.51 |    0.00 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    47.519 μs |   4.6980 μs |  0.2575 μs |   4.15 |    0.02 |    5 | 1.2207 |  20.04 KB |        3.11 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.753 μs** |   **0.2871 μs** |  **0.0157 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.554 μs |   0.1625 μs |  0.0089 μs |   0.81 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.811 μs |   2.3106 μs |  0.1267 μs |   4.75 |    0.01 |    7 | 1.2817 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    13.312 μs |   0.2512 μs |  0.0138 μs |   1.13 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.870 μs |   1.5492 μs |  0.0849 μs |   2.54 |    0.01 |    6 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    56.152 μs |   2.1914 μs |  0.1201 μs |   4.78 |    0.01 |    7 | 1.2817 |  21.06 KB |        3.27 |
 'Keyset pagination'                  | 10            |    21.278 μs |   0.3775 μs |  0.0207 μs |   1.81 |    0.00 |    5 | 0.6409 |  10.74 KB |        1.67 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,273.575 μs | 587.7494 μs | 32.2165 μs | 278.53 |    2.40 |    8 | 3.9063 | 109.69 KB |       17.02 |
 'Lambda Include'                     | 10            |     7.227 μs |   0.0974 μs |  0.0053 μs |   0.61 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.198 μs |   0.1882 μs |  0.0103 μs |   0.61 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.485 μs |   7.0062 μs |  0.3840 μs |   1.40 |    0.03 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    17.167 μs |   1.8585 μs |  0.1019 μs |   1.46 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    46.758 μs |   3.9902 μs |  0.2187 μs |   3.98 |    0.02 |    7 | 1.2207 |  20.04 KB |        3.11 |
