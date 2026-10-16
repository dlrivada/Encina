
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **7.573 μs** |    **16.0802 μs** |   **0.8814 μs** |   **1.01** |    **0.14** |    **3** | **0.3891** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     6.034 μs |     7.2348 μs |   0.3966 μs |   0.80 |    0.09 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |     8.541 μs |     2.2457 μs |   0.1231 μs |   1.14 |    0.11 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |     8.784 μs |     0.2701 μs |   0.0148 μs |   1.17 |    0.11 |    3 | 0.3815 |   6.44 KB |        1.00 |
 'Five criteria (AND)'                | 2             |    19.424 μs |     3.0134 μs |   0.1652 μs |   2.59 |    0.25 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    38.429 μs |     2.5797 μs |   0.1414 μs |   5.12 |    0.49 |    7 | 1.2817 |  21.21 KB |        3.29 |
 'Keyset pagination'                  | 2             |    12.207 μs |     2.1827 μs |   0.1196 μs |   1.63 |    0.16 |    4 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 2,581.969 μs |   307.0779 μs |  16.8320 μs | 343.87 |   32.83 |    8 | 3.9063 | 108.92 KB |       16.90 |
 'Lambda Include'                     | 2             |     4.445 μs |     0.3497 μs |   0.0192 μs |   0.59 |    0.06 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     4.633 μs |     0.6294 μs |   0.0345 μs |   0.62 |    0.06 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    10.933 μs |     5.6214 μs |   0.3081 μs |   1.46 |    0.14 |    4 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    11.317 μs |     0.9618 μs |   0.0527 μs |   1.51 |    0.14 |    4 | 0.5646 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    29.148 μs |     6.3471 μs |   0.3479 μs |   3.88 |    0.37 |    6 | 1.1902 |  19.88 KB |        3.08 |
                                      |               |              |               |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **7.172 μs** |     **0.3751 μs** |   **0.0206 μs** |   **1.00** |    **0.00** |    **3** | **0.3891** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     5.793 μs |     0.5618 μs |   0.0308 μs |   0.81 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    37.922 μs |     3.8388 μs |   0.2104 μs |   5.29 |    0.03 |    7 | 1.2817 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |     8.637 μs |     4.7355 μs |   0.2596 μs |   1.20 |    0.03 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    19.681 μs |     1.3879 μs |   0.0761 μs |   2.74 |    0.01 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    37.226 μs |     1.4574 μs |   0.0799 μs |   5.19 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    12.878 μs |     1.2671 μs |   0.0695 μs |   1.80 |    0.01 |    4 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 2,755.202 μs | 2,484.1661 μs | 136.1655 μs | 384.17 |   16.47 |    8 | 3.9063 |  109.3 KB |       16.96 |
 'Lambda Include'                     | 10            |     4.740 μs |     2.9722 μs |   0.1629 μs |   0.66 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     4.713 μs |     0.5890 μs |   0.0323 μs |   0.66 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    10.941 μs |     3.7839 μs |   0.2074 μs |   1.53 |    0.03 |    4 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    11.253 μs |     1.6612 μs |   0.0911 μs |   1.57 |    0.01 |    4 | 0.5646 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    29.276 μs |    12.9393 μs |   0.7092 μs |   4.08 |    0.09 |    6 | 1.1902 |  19.88 KB |        3.08 |
