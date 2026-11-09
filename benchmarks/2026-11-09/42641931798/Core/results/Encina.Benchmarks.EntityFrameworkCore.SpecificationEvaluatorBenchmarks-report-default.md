
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.689 μs** |   **1.2308 μs** |  **0.0675 μs** |   **1.00** |    **0.01** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.543 μs |   0.0787 μs |  0.0043 μs |   0.82 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    13.209 μs |   2.0721 μs |  0.1136 μs |   1.13 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.218 μs |   1.3464 μs |  0.0738 μs |   1.13 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.569 μs |   0.9850 μs |  0.0540 μs |   2.53 |    0.01 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    55.433 μs |   4.2469 μs |  0.2328 μs |   4.74 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    19.989 μs |   1.9342 μs |  0.1060 μs |   1.71 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,222.731 μs | 295.4176 μs | 16.1928 μs | 275.70 |    1.83 |    7 | 3.9063 | 109.67 KB |       17.02 |
 'Lambda Include'                     | 2             |     7.291 μs |   0.8266 μs |  0.0453 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.295 μs |   1.6408 μs |  0.0899 μs |   0.62 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    16.379 μs |   1.5860 μs |  0.0869 μs |   1.40 |    0.01 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    17.276 μs |   0.7682 μs |  0.0421 μs |   1.48 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    46.247 μs |   2.5162 μs |  0.1379 μs |   3.96 |    0.02 |    6 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.284 μs** |   **0.9956 μs** |  **0.0546 μs** |   **1.00** |    **0.01** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.449 μs |   1.2521 μs |  0.0686 μs |   0.84 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.803 μs |   2.5008 μs |  0.1371 μs |   4.95 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.184 μs |   0.8400 μs |  0.0460 μs |   1.17 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.159 μs |   1.8439 μs |  0.1011 μs |   2.58 |    0.01 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    56.174 μs |   4.4939 μs |  0.2463 μs |   4.98 |    0.03 |    7 | 1.2817 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 10            |    20.914 μs |   3.3028 μs |  0.1810 μs |   1.85 |    0.02 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,248.035 μs | 424.2310 μs | 23.2535 μs | 287.85 |    2.15 |    8 | 3.9063 | 109.02 KB |       16.91 |
 'Lambda Include'                     | 10            |     7.215 μs |   0.6865 μs |  0.0376 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.107 μs |   0.2099 μs |  0.0115 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.499 μs |   2.7938 μs |  0.1531 μs |   1.46 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    17.188 μs |   0.9809 μs |  0.0538 μs |   1.52 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    45.910 μs |   5.6088 μs |  0.3074 μs |   4.07 |    0.03 |    6 | 1.1597 |  19.88 KB |        3.08 |
