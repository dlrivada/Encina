
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **6.826 μs** |     **1.5708 μs** |  **0.0861 μs** |   **1.00** |    **0.02** |    **3** | **0.3891** |      **-** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     5.670 μs |     1.3447 μs |  0.0737 μs |   0.83 |    0.01 |    2 | 0.3204 |      - |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |     8.721 μs |     5.9751 μs |  0.3275 μs |   1.28 |    0.04 |    3 | 0.3815 |      - |   6.44 KB |        1.00 |
 'Two criteria (AND)'                 | 2             |     8.124 μs |     0.9221 μs |  0.0505 μs |   1.19 |    0.01 |    3 | 0.3662 |      - |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    18.957 μs |     1.6892 μs |  0.0926 μs |   2.78 |    0.03 |    5 | 0.7019 |      - |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    37.786 μs |     7.7721 μs |  0.4260 μs |   5.54 |    0.08 |    7 | 1.2817 |      - |  21.06 KB |        3.27 |
 'Keyset pagination'                  | 2             |    12.706 μs |     1.8819 μs |  0.1032 μs |   1.86 |    0.02 |    4 | 0.6256 |      - |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 2,580.484 μs | 1,727.4676 μs | 94.6883 μs | 378.10 |   12.71 |    8 | 3.9063 |      - | 109.04 KB |       16.92 |
 'Lambda Include'                     | 2             |     4.605 μs |     0.9100 μs |  0.0499 μs |   0.67 |    0.01 |    1 | 0.2594 |      - |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     4.520 μs |     0.6882 μs |  0.0377 μs |   0.66 |    0.01 |    1 | 0.2594 |      - |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    10.736 μs |     1.6556 μs |  0.0907 μs |   1.57 |    0.02 |    4 | 0.5341 |      - |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    11.115 μs |     0.9556 μs |  0.0524 μs |   1.63 |    0.02 |    4 | 0.5646 |      - |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    28.822 μs |     3.6462 μs |  0.1999 μs |   4.22 |    0.05 |    6 | 1.2207 | 0.0305 |  20.03 KB |        3.11 |
                                      |               |              |               |            |        |         |      |        |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **7.084 μs** |     **2.2602 μs** |  **0.1239 μs** |   **1.00** |    **0.02** |    **3** | **0.3891** |      **-** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     5.761 μs |     0.7573 μs |  0.0415 μs |   0.81 |    0.01 |    2 | 0.3204 |      - |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    37.765 μs |     2.5621 μs |  0.1404 μs |   5.33 |    0.08 |    7 | 1.2207 |      - |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |     8.511 μs |     5.0044 μs |  0.2743 μs |   1.20 |    0.04 |    3 | 0.3662 |      - |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    19.978 μs |     9.7926 μs |  0.5368 μs |   2.82 |    0.08 |    5 | 0.7019 |      - |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    36.670 μs |    15.6908 μs |  0.8601 μs |   5.18 |    0.13 |    7 | 1.2207 |      - |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    12.793 μs |     4.8829 μs |  0.2676 μs |   1.81 |    0.04 |    4 | 0.6256 |      - |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 2,700.692 μs |   968.7318 μs | 53.0995 μs | 381.31 |    8.73 |    8 | 3.9063 |      - | 109.18 KB |       16.94 |
 'Lambda Include'                     | 10            |     4.797 μs |     3.5303 μs |  0.1935 μs |   0.68 |    0.03 |    1 | 0.2594 |      - |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     4.667 μs |     1.1700 μs |  0.0641 μs |   0.66 |    0.01 |    1 | 0.2594 |      - |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    10.804 μs |     3.3685 μs |  0.1846 μs |   1.53 |    0.03 |    4 | 0.5341 |      - |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    11.737 μs |     0.7507 μs |  0.0411 μs |   1.66 |    0.03 |    4 | 0.5798 |      - |   9.67 KB |        1.50 |
 'Full specification (all features)'  | 10            |    29.610 μs |     6.0266 μs |  0.3303 μs |   4.18 |    0.08 |    6 | 1.1902 |      - |  19.88 KB |        3.08 |
