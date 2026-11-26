
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **9.441 μs** |     **1.8637 μs** |   **0.1022 μs** |   **1.00** |    **0.01** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     8.026 μs |     0.4236 μs |   0.0232 μs |   0.85 |    0.01 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    11.171 μs |    12.1081 μs |   0.6637 μs |   1.18 |    0.06 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    10.801 μs |     1.5511 μs |   0.0850 μs |   1.14 |    0.01 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    24.462 μs |     0.3420 μs |   0.0187 μs |   2.59 |    0.02 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    45.780 μs |     2.9138 μs |   0.1597 μs |   4.85 |    0.05 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    16.875 μs |     0.2595 μs |   0.0142 μs |   1.79 |    0.02 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,281.928 μs | 3,764.3633 μs | 206.3375 μs | 347.66 |   19.20 |    6 |      - | 109.32 KB |       16.96 |
 'Lambda Include'                     | 2             |     6.078 μs |     7.8228 μs |   0.4288 μs |   0.64 |    0.04 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     5.999 μs |     0.4902 μs |   0.0269 μs |   0.64 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    13.781 μs |     0.4873 μs |   0.0267 μs |   1.46 |    0.01 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    15.060 μs |     0.6897 μs |   0.0378 μs |   1.60 |    0.02 |    3 | 0.1068 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    45.317 μs |    17.2628 μs |   0.9462 μs |   4.80 |    0.10 |    5 | 0.2441 |  20.18 KB |        3.13 |
                                      |               |              |               |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **9.512 μs** |     **6.2625 μs** |   **0.3433 μs** |   **1.00** |    **0.04** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     8.979 μs |    10.1634 μs |   0.5571 μs |   0.94 |    0.06 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    46.177 μs |    14.9584 μs |   0.8199 μs |   4.86 |    0.17 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    10.789 μs |     1.8197 μs |   0.0997 μs |   1.14 |    0.04 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    24.218 μs |     2.4556 μs |   0.1346 μs |   2.55 |    0.08 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    45.704 μs |     3.4373 μs |   0.1884 μs |   4.81 |    0.15 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    16.703 μs |     0.4714 μs |   0.0258 μs |   1.76 |    0.05 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,010.797 μs |   256.0097 μs |  14.0328 μs | 316.80 |    9.78 |    6 |      - | 110.31 KB |       17.11 |
 'Lambda Include'                     | 10            |     6.091 μs |     4.7449 μs |   0.2601 μs |   0.64 |    0.03 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     6.152 μs |     5.2710 μs |   0.2889 μs |   0.65 |    0.03 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    13.866 μs |     1.5793 μs |   0.0866 μs |   1.46 |    0.05 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    15.065 μs |    10.9697 μs |   0.6013 μs |   1.59 |    0.07 |    3 | 0.1068 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    39.330 μs |     1.5841 μs |   0.0868 μs |   4.14 |    0.13 |    5 | 0.1831 |  19.88 KB |        3.08 |
