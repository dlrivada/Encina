
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **14.833 μs** |   **1.0396 μs** |  **0.0570 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    12.115 μs |   0.9603 μs |  0.0526 μs |   0.82 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    17.078 μs |   1.9329 μs |  0.1059 μs |   1.15 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    16.898 μs |   0.8659 μs |  0.0475 μs |   1.14 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    38.117 μs |   1.6267 μs |  0.0892 μs |   2.57 |    0.01 |    5 | 0.6714 |  11.71 KB |        1.82 |
 'Ten criteria (AND)'                 | 2             |    70.696 μs |   9.4909 μs |  0.5202 μs |   4.77 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    26.259 μs |   0.6218 μs |  0.0341 μs |   1.77 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,121.067 μs | 449.8976 μs | 24.6604 μs | 277.83 |    1.71 |    7 |      - | 109.01 KB |       16.91 |
 'Lambda Include'                     | 2             |     9.309 μs |   0.3432 μs |  0.0188 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     9.214 μs |   0.1950 μs |  0.0107 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    20.945 μs |   0.5819 μs |  0.0319 μs |   1.41 |    0.01 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    22.186 μs |   2.0447 μs |  0.1121 μs |   1.50 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    58.848 μs |   1.6715 μs |  0.0916 μs |   3.97 |    0.01 |    6 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **14.447 μs** |   **0.5538 μs** |  **0.0304 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    12.145 μs |   1.6793 μs |  0.0921 μs |   0.84 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    72.125 μs |   9.8705 μs |  0.5410 μs |   4.99 |    0.03 |    5 | 1.2207 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    17.315 μs |   0.6716 μs |  0.0368 μs |   1.20 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    38.295 μs |   0.9087 μs |  0.0498 μs |   2.65 |    0.01 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    72.170 μs |   5.9309 μs |  0.3251 μs |   5.00 |    0.02 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    25.904 μs |   2.3738 μs |  0.1301 μs |   1.79 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,148.576 μs | 451.8157 μs | 24.7655 μs | 287.16 |    1.57 |    6 |      - | 110.22 KB |       17.10 |
 'Lambda Include'                     | 10            |     9.241 μs |   0.3454 μs |  0.0189 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     9.180 μs |   0.9022 μs |  0.0495 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    21.300 μs |   1.3094 μs |  0.0718 μs |   1.47 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    21.966 μs |   5.1303 μs |  0.2812 μs |   1.52 |    0.02 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    61.001 μs |   0.7905 μs |  0.0433 μs |   4.22 |    0.01 |    5 | 1.2207 |  20.03 KB |        3.11 |
