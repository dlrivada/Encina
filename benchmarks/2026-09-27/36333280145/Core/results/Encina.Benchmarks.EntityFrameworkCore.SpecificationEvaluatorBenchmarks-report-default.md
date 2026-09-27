
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **9.377 μs** |     **0.3072 μs** |  **0.0168 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     8.306 μs |     5.5505 μs |  0.3042 μs |   0.89 |    0.03 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    10.620 μs |     0.3370 μs |  0.0185 μs |   1.13 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    10.748 μs |     0.5793 μs |  0.0318 μs |   1.15 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    24.542 μs |     1.3496 μs |  0.0740 μs |   2.62 |    0.01 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    47.005 μs |    15.0832 μs |  0.8268 μs |   5.01 |    0.08 |    6 | 0.2441 |  21.23 KB |        3.29 |
 'Keyset pagination'                  | 2             |    17.678 μs |     0.8902 μs |  0.0488 μs |   1.89 |    0.01 |    3 | 0.1221 |  10.74 KB |        1.67 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,060.938 μs |   523.4261 μs | 28.6908 μs | 326.42 |    2.70 |    7 |      - | 109.35 KB |       16.97 |
 'Lambda Include'                     | 2             |     5.853 μs |     0.3910 μs |  0.0214 μs |   0.62 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     5.893 μs |     3.6480 μs |  0.2000 μs |   0.63 |    0.02 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    14.791 μs |     9.5335 μs |  0.5226 μs |   1.58 |    0.05 |    3 | 0.1068 |   9.09 KB |        1.41 |
 'Offset pagination (Skip/Take)'      | 2             |    14.534 μs |     1.9533 μs |  0.1071 μs |   1.55 |    0.01 |    3 | 0.1068 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    38.688 μs |     2.8529 μs |  0.1564 μs |   4.13 |    0.02 |    5 | 0.1831 |  19.88 KB |        3.08 |
                                      |               |              |               |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **9.597 μs** |     **1.2806 μs** |  **0.0702 μs** |   **1.00** |    **0.01** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     8.318 μs |     0.2581 μs |  0.0141 μs |   0.87 |    0.01 |    2 | 0.0610 |   5.38 KB |        0.83 |
 'Complex predicates (parameterized)' | 10            |    46.101 μs |     1.7716 μs |  0.0971 μs |   4.80 |    0.03 |    5 | 0.2441 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    11.175 μs |     6.3974 μs |  0.3507 μs |   1.16 |    0.03 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    24.605 μs |     3.0826 μs |  0.1690 μs |   2.56 |    0.02 |    4 | 0.1221 |  11.84 KB |        1.84 |
 'Ten criteria (AND)'                 | 10            |    45.020 μs |     1.7877 μs |  0.0980 μs |   4.69 |    0.03 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    17.154 μs |     0.9539 μs |  0.0523 μs |   1.79 |    0.01 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,044.353 μs | 1,137.2491 μs | 62.3365 μs | 317.25 |    5.97 |    6 |      - | 108.77 KB |       16.88 |
 'Lambda Include'                     | 10            |     5.825 μs |     0.2452 μs |  0.0134 μs |   0.61 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     5.904 μs |     1.7196 μs |  0.0943 μs |   0.62 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    13.618 μs |     0.6595 μs |  0.0361 μs |   1.42 |    0.01 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    15.235 μs |     6.5624 μs |  0.3597 μs |   1.59 |    0.03 |    3 | 0.1068 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    39.206 μs |     0.7840 μs |  0.0430 μs |   4.09 |    0.03 |    5 | 0.1831 |  19.88 KB |        3.08 |
