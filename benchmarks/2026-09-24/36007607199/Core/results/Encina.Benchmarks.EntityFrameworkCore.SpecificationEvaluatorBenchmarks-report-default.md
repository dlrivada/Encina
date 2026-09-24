
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.049 μs** |   **0.8316 μs** |  **0.0456 μs** |   **1.00** |    **0.01** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.358 μs |   0.2524 μs |  0.0138 μs |   0.85 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    12.732 μs |   1.4318 μs |  0.0785 μs |   1.15 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.050 μs |   0.8326 μs |  0.0456 μs |   1.18 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.206 μs |   4.2861 μs |  0.2349 μs |   2.64 |    0.02 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    55.029 μs |   0.6955 μs |  0.0381 μs |   4.98 |    0.02 |    5 | 1.2817 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    19.838 μs |   1.9464 μs |  0.1067 μs |   1.80 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,208.164 μs | 111.9457 μs |  6.1361 μs | 290.35 |    1.14 |    6 | 3.9063 | 109.36 KB |       16.97 |
 'Lambda Include'                     | 2             |     7.001 μs |   0.2391 μs |  0.0131 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.002 μs |   0.6907 μs |  0.0379 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    15.982 μs |   0.8408 μs |  0.0461 μs |   1.45 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    16.629 μs |   1.1162 μs |  0.0612 μs |   1.50 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    45.924 μs |   4.9654 μs |  0.2722 μs |   4.16 |    0.03 |    5 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.401 μs** |   **0.3127 μs** |  **0.0171 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.257 μs |   0.2612 μs |  0.0143 μs |   0.81 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.464 μs |   3.0482 μs |  0.1671 μs |   4.86 |    0.01 |    8 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.010 μs |   1.1939 μs |  0.0654 μs |   1.14 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.446 μs |   1.7210 μs |  0.0943 μs |   2.58 |    0.01 |    6 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    54.891 μs |   6.5754 μs |  0.3604 μs |   4.81 |    0.03 |    8 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    20.466 μs |   1.6947 μs |  0.0929 μs |   1.80 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,233.227 μs | 392.5814 μs | 21.5187 μs | 283.59 |    1.68 |    9 | 3.9063 | 109.93 KB |       17.06 |
 'Lambda Include'                     | 10            |     6.933 μs |   0.3389 μs |  0.0186 μs |   0.61 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.490 μs |   0.6665 μs |  0.0365 μs |   0.66 |    0.00 |    1 | 0.2747 |   4.55 KB |        0.71 |
 'Multi-column ordering'              | 10            |    16.142 μs |   4.2504 μs |  0.2330 μs |   1.42 |    0.02 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    16.966 μs |   0.2921 μs |  0.0160 μs |   1.49 |    0.00 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    45.215 μs |   2.7360 μs |  0.1500 μs |   3.97 |    0.01 |    7 | 1.1597 |  19.88 KB |        3.08 |
