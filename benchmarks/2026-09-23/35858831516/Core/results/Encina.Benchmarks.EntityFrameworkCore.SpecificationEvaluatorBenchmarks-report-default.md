
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **9.838 μs** |     **6.5600 μs** |   **0.3596 μs** |   **1.00** |    **0.04** |    **3** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     8.052 μs |     1.2603 μs |   0.0691 μs |   0.82 |    0.03 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    11.163 μs |     6.4177 μs |   0.3518 μs |   1.14 |    0.05 |    3 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    11.205 μs |     8.8715 μs |   0.4863 μs |   1.14 |    0.06 |    3 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    26.097 μs |    22.4110 μs |   1.2284 μs |   2.65 |    0.14 |    5 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    46.022 μs |     6.7396 μs |   0.3694 μs |   4.68 |    0.15 |    6 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    17.044 μs |     4.1641 μs |   0.2282 μs |   1.73 |    0.06 |    4 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,212.396 μs | 2,617.1828 μs | 143.4566 μs | 326.81 |   16.23 |    7 |      - | 109.34 KB |       16.96 |
 'Lambda Include'                     | 2             |     6.130 μs |     2.7393 μs |   0.1501 μs |   0.62 |    0.02 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     6.174 μs |     3.9348 μs |   0.2157 μs |   0.63 |    0.03 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    13.882 μs |     1.2286 μs |   0.0673 μs |   1.41 |    0.04 |    4 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    14.309 μs |     1.0704 μs |   0.0587 μs |   1.46 |    0.05 |    4 | 0.1068 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    39.554 μs |     1.4131 μs |   0.0775 μs |   4.02 |    0.13 |    6 | 0.2441 |  20.32 KB |        3.15 |
                                      |               |              |               |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **9.461 μs** |     **0.3804 μs** |   **0.0209 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     8.044 μs |     1.2109 μs |   0.0664 μs |   0.85 |    0.01 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    47.756 μs |    54.9180 μs |   3.0102 μs |   5.05 |    0.28 |    5 | 0.2441 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    11.241 μs |    11.6569 μs |   0.6390 μs |   1.19 |    0.06 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    25.321 μs |    11.3384 μs |   0.6215 μs |   2.68 |    0.06 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    45.928 μs |    10.1076 μs |   0.5540 μs |   4.85 |    0.05 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    16.807 μs |     0.7788 μs |   0.0427 μs |   1.78 |    0.01 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,236.192 μs | 1,948.9468 μs | 106.8284 μs | 342.07 |    9.80 |    6 |      - |  109.4 KB |       16.97 |
 'Lambda Include'                     | 10            |     6.266 μs |     6.2806 μs |   0.3443 μs |   0.66 |    0.03 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     5.999 μs |     2.7367 μs |   0.1500 μs |   0.63 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    14.135 μs |     4.5328 μs |   0.2485 μs |   1.49 |    0.02 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    14.840 μs |     8.8324 μs |   0.4841 μs |   1.57 |    0.04 |    3 | 0.1068 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    40.055 μs |    34.5750 μs |   1.8952 μs |   4.23 |    0.17 |    5 | 0.1831 |  19.88 KB |        3.08 |
