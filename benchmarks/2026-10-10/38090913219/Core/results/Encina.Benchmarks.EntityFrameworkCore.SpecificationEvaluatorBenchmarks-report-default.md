
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.103 μs** |   **0.6659 μs** | **0.0365 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.325 μs |   0.5785 μs | 0.0317 μs |   0.84 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    12.921 μs |   3.1609 μs | 0.1733 μs |   1.16 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    12.834 μs |   1.1961 μs | 0.0656 μs |   1.16 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.699 μs |   1.4681 μs | 0.0805 μs |   2.67 |    0.01 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    54.840 μs |   1.1618 μs | 0.0637 μs |   4.94 |    0.01 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    19.529 μs |   0.6920 μs | 0.0379 μs |   1.76 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,214.341 μs | 180.4576 μs | 9.8915 μs | 289.50 |    1.13 |    6 | 3.9063 | 109.74 KB |       17.03 |
 'Lambda Include'                     | 2             |     6.971 μs |   0.3444 μs | 0.0189 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     6.921 μs |   0.7780 μs | 0.0426 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    15.752 μs |   1.2185 μs | 0.0668 μs |   1.42 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    17.432 μs |   1.0391 μs | 0.0570 μs |   1.57 |    0.01 |    3 | 0.5798 |   9.67 KB |        1.50 |
 'Full specification (all features)'  | 2             |    46.611 μs |  14.4733 μs | 0.7933 μs |   4.20 |    0.06 |    5 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.461 μs** |   **0.6395 μs** | **0.0351 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.470 μs |   0.2881 μs | 0.0158 μs |   0.83 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.041 μs |   2.5129 μs | 0.1377 μs |   4.80 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.357 μs |   0.4505 μs | 0.0247 μs |   1.17 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.739 μs |   0.8700 μs | 0.0477 μs |   2.59 |    0.01 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    55.505 μs |   0.3152 μs | 0.0173 μs |   4.84 |    0.01 |    6 | 1.2817 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 10            |    20.458 μs |   7.9250 μs | 0.4344 μs |   1.79 |    0.03 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,217.035 μs |  91.2146 μs | 4.9998 μs | 280.71 |    0.83 |    7 | 3.9063 | 109.82 KB |       17.04 |
 'Lambda Include'                     | 10            |     7.109 μs |   0.7933 μs | 0.0435 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.069 μs |   0.2749 μs | 0.0151 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.106 μs |   0.5738 μs | 0.0315 μs |   1.41 |    0.00 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    17.020 μs |   2.4287 μs | 0.1331 μs |   1.49 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    46.008 μs |   4.4792 μs | 0.2455 μs |   4.01 |    0.02 |    6 | 1.1597 |  19.88 KB |        3.08 |
