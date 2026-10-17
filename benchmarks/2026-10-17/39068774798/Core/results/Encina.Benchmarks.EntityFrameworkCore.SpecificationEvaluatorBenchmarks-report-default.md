
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **14.509 μs** |   **0.9106 μs** |  **0.0499 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    12.109 μs |   0.4874 μs |  0.0267 μs |   0.83 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    17.149 μs |   0.8737 μs |  0.0479 μs |   1.18 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    17.124 μs |   0.8387 μs |  0.0460 μs |   1.18 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    37.967 μs |   1.9475 μs |  0.1068 μs |   2.62 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    71.377 μs |   8.7050 μs |  0.4771 μs |   4.92 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    26.570 μs |   2.9116 μs |  0.1596 μs |   1.83 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,140.378 μs | 605.6058 μs | 33.1953 μs | 285.37 |    2.16 |    7 |      - | 108.64 KB |       16.86 |
 'Lambda Include'                     | 2             |     9.272 μs |   0.2543 μs |  0.0139 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     9.206 μs |   0.2136 μs |  0.0117 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    20.576 μs |   0.4732 μs |  0.0259 μs |   1.42 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    22.104 μs |   5.5906 μs |  0.3064 μs |   1.52 |    0.02 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    59.683 μs |   5.6224 μs |  0.3082 μs |   4.11 |    0.02 |    6 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **14.337 μs** |   **0.2147 μs** |  **0.0118 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    12.384 μs |   1.1146 μs |  0.0611 μs |   0.86 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    70.803 μs |   4.9421 μs |  0.2709 μs |   4.94 |    0.02 |    6 | 1.2207 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    17.002 μs |   0.1916 μs |  0.0105 μs |   1.19 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    37.660 μs |   0.6769 μs |  0.0371 μs |   2.63 |    0.00 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    71.579 μs |   2.4508 μs |  0.1343 μs |   4.99 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    25.907 μs |   2.5843 μs |  0.1417 μs |   1.81 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,138.435 μs | 437.3867 μs | 23.9746 μs | 288.65 |    1.46 |    7 |      - | 109.31 KB |       16.96 |
 'Lambda Include'                     | 10            |     9.134 μs |   0.6719 μs |  0.0368 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     9.202 μs |   0.7470 μs |  0.0409 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    21.012 μs |   0.1435 μs |  0.0079 μs |   1.47 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    21.991 μs |   0.8769 μs |  0.0481 μs |   1.53 |    0.00 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    58.657 μs |   3.3161 μs |  0.1818 μs |   4.09 |    0.01 |    5 | 1.1597 |  19.88 KB |        3.08 |
