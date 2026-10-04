
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **9.550 μs** |     **1.1770 μs** |   **0.0645 μs** |   **1.00** |    **0.01** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     8.554 μs |     8.9135 μs |   0.4886 μs |   0.90 |    0.04 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    10.916 μs |     5.0223 μs |   0.2753 μs |   1.14 |    0.03 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    10.786 μs |     0.3753 μs |   0.0206 μs |   1.13 |    0.01 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    26.635 μs |    44.1399 μs |   2.4195 μs |   2.79 |    0.22 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    45.694 μs |     9.7145 μs |   0.5325 μs |   4.78 |    0.06 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    16.829 μs |     0.3030 μs |   0.0166 μs |   1.76 |    0.01 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,244.490 μs | 1,723.5505 μs |  94.4736 μs | 339.74 |    8.79 |    6 |      - | 109.64 KB |       17.01 |
 'Lambda Include'                     | 2             |     6.050 μs |     2.1314 μs |   0.1168 μs |   0.63 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     5.829 μs |     1.0431 μs |   0.0572 μs |   0.61 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    13.711 μs |     1.7564 μs |   0.0963 μs |   1.44 |    0.01 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    14.677 μs |     0.2281 μs |   0.0125 μs |   1.54 |    0.01 |    3 | 0.1068 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    39.232 μs |     0.4274 μs |   0.0234 μs |   4.11 |    0.02 |    5 | 0.2441 |  20.17 KB |        3.13 |
                                      |               |              |               |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **10.151 μs** |     **4.2946 μs** |   **0.2354 μs** |   **1.00** |    **0.03** |    **3** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     8.075 μs |     0.7676 μs |   0.0421 μs |   0.80 |    0.02 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    47.417 μs |    33.4995 μs |   1.8362 μs |   4.67 |    0.18 |    7 | 0.2441 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    11.253 μs |    13.0872 μs |   0.7174 μs |   1.11 |    0.07 |    3 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    26.549 μs |    27.8134 μs |   1.5245 μs |   2.62 |    0.14 |    6 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    45.250 μs |     1.2386 μs |   0.0679 μs |   4.46 |    0.09 |    7 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    18.682 μs |    39.8371 μs |   2.1836 μs |   1.84 |    0.19 |    5 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,262.637 μs | 3,942.0457 μs | 216.0768 μs | 321.51 |   19.52 |    8 |      - | 110.52 KB |       17.15 |
 'Lambda Include'                     | 10            |     6.099 μs |     5.3132 μs |   0.2912 μs |   0.60 |    0.03 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     6.054 μs |     0.3417 μs |   0.0187 μs |   0.60 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    13.796 μs |     1.3922 μs |   0.0763 μs |   1.36 |    0.03 |    4 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    14.498 μs |     0.9309 μs |   0.0510 μs |   1.43 |    0.03 |    4 | 0.1068 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    40.548 μs |    26.5075 μs |   1.4530 μs |   4.00 |    0.15 |    7 | 0.1831 |  19.88 KB |        3.08 |
