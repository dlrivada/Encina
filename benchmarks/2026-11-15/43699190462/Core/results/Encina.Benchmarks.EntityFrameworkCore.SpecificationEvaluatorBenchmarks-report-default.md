
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.563 μs** |   **0.3194 μs** |  **0.0175 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.398 μs |   0.5500 μs |  0.0301 μs |   0.81 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    13.376 μs |   1.9750 μs |  0.1083 μs |   1.16 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.555 μs |   0.8537 μs |  0.0468 μs |   1.17 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.971 μs |   1.0728 μs |  0.0588 μs |   2.59 |    0.01 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    55.760 μs |   2.8662 μs |  0.1571 μs |   4.82 |    0.01 |    6 | 1.2817 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    21.014 μs |   1.1908 μs |  0.0653 μs |   1.82 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,256.570 μs | 179.8390 μs |  9.8576 μs | 281.64 |    0.83 |    7 | 3.9063 | 108.87 KB |       16.89 |
 'Lambda Include'                     | 2             |     7.127 μs |   0.7236 μs |  0.0397 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.131 μs |   0.7696 μs |  0.0422 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    16.199 μs |   2.6614 μs |  0.1459 μs |   1.40 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    17.175 μs |   0.5440 μs |  0.0298 μs |   1.49 |    0.00 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    46.479 μs |   0.7900 μs |  0.0433 μs |   4.02 |    0.01 |    6 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.447 μs** |   **0.2925 μs** |  **0.0160 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.545 μs |   0.1630 μs |  0.0089 μs |   0.83 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.749 μs |  12.4660 μs |  0.6833 μs |   4.87 |    0.05 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.377 μs |   0.7885 μs |  0.0432 μs |   1.17 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.797 μs |   1.7028 μs |  0.0933 μs |   2.60 |    0.01 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    56.538 μs |   4.9431 μs |  0.2709 μs |   4.94 |    0.02 |    7 | 1.2817 |  21.06 KB |        3.27 |
 'Keyset pagination'                  | 10            |    21.589 μs |   9.0764 μs |  0.4975 μs |   1.89 |    0.04 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,334.174 μs | 821.6131 μs | 45.0354 μs | 291.26 |    3.43 |    8 | 3.9063 | 109.14 KB |       16.93 |
 'Lambda Include'                     | 10            |     7.256 μs |   0.7189 μs |  0.0394 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.277 μs |   1.9204 μs |  0.1053 μs |   0.64 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.550 μs |   0.8964 μs |  0.0491 μs |   1.45 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    17.306 μs |   1.4634 μs |  0.0802 μs |   1.51 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    46.465 μs |   9.7455 μs |  0.5342 μs |   4.06 |    0.04 |    6 | 1.1597 |  19.88 KB |        3.08 |
