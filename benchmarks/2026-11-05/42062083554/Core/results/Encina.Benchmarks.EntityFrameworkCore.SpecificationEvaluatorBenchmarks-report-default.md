
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **7.660 μs** |     **0.4560 μs** |  **0.0250 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     6.260 μs |     1.6456 μs |  0.0902 μs |   0.82 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |     9.379 μs |     9.9932 μs |  0.5478 μs |   1.22 |    0.06 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |     8.965 μs |     1.7782 μs |  0.0975 μs |   1.17 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    20.908 μs |    11.1399 μs |  0.6106 μs |   2.73 |    0.07 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    41.004 μs |    60.9134 μs |  3.3389 μs |   5.35 |    0.38 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    13.545 μs |     1.3437 μs |  0.0737 μs |   1.77 |    0.01 |    4 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,131.815 μs | 1,009.3610 μs | 55.3265 μs | 408.87 |    6.36 |    8 | 3.9063 | 109.17 KB |       16.94 |
 'Lambda Include'                     | 2             |     5.008 μs |     0.3659 μs |  0.0201 μs |   0.65 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     5.104 μs |     1.5194 μs |  0.0833 μs |   0.67 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    11.741 μs |     1.5734 μs |  0.0862 μs |   1.53 |    0.01 |    4 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    11.773 μs |     0.3928 μs |  0.0215 μs |   1.54 |    0.00 |    4 | 0.5646 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    31.092 μs |     1.9478 μs |  0.1068 μs |   4.06 |    0.02 |    6 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |               |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **7.582 μs** |     **0.4408 μs** |  **0.0242 μs** |   **1.00** |    **0.00** |    **2** | **0.3891** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     6.352 μs |     0.6185 μs |  0.0339 μs |   0.84 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    39.166 μs |     2.7865 μs |  0.1527 μs |   5.17 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |     8.994 μs |     0.2126 μs |  0.0117 μs |   1.19 |    0.00 |    2 | 0.3815 |   6.29 KB |        0.98 |
 'Five criteria (AND)'                | 10            |    20.435 μs |     0.8551 μs |  0.0469 μs |   2.70 |    0.01 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    38.031 μs |     4.5383 μs |  0.2488 μs |   5.02 |    0.03 |    6 | 1.2817 |  21.06 KB |        3.27 |
 'Keyset pagination'                  | 10            |    12.926 μs |     0.9828 μs |  0.0539 μs |   1.70 |    0.01 |    3 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 2,583.959 μs |   513.6320 μs | 28.1539 μs | 340.81 |    3.35 |    7 | 3.9063 | 109.71 KB |       17.02 |
 'Lambda Include'                     | 10            |     4.631 μs |     0.4482 μs |  0.0246 μs |   0.61 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     4.675 μs |     0.1059 μs |  0.0058 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    10.835 μs |     0.7909 μs |  0.0434 μs |   1.43 |    0.01 |    3 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    11.727 μs |     1.7823 μs |  0.0977 μs |   1.55 |    0.01 |    3 | 0.5798 |   9.66 KB |        1.50 |
 'Full specification (all features)'  | 10            |    29.400 μs |     4.2678 μs |  0.2339 μs |   3.88 |    0.03 |    5 | 1.1597 |  19.88 KB |        3.08 |
