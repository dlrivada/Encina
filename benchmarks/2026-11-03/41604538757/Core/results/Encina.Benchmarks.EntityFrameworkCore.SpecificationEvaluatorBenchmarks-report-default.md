
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **6.974 μs** |   **0.3222 μs** |  **0.0177 μs** |   **1.00** |    **0.00** |    **3** | **0.3891** |      **-** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     5.661 μs |   1.5882 μs |  0.0871 μs |   0.81 |    0.01 |    2 | 0.3204 |      - |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |     8.210 μs |   6.2854 μs |  0.3445 μs |   1.18 |    0.04 |    3 | 0.3662 |      - |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |     8.009 μs |   0.8404 μs |  0.0461 μs |   1.15 |    0.01 |    3 | 0.3662 |      - |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    18.921 μs |   1.3823 μs |  0.0758 μs |   2.71 |    0.01 |    5 | 0.7019 |      - |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    39.607 μs |  45.9338 μs |  2.5178 μs |   5.68 |    0.31 |    7 | 1.2207 |      - |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    12.468 μs |   1.9143 μs |  0.1049 μs |   1.79 |    0.01 |    4 | 0.6256 |      - |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 2,547.134 μs | 425.0219 μs | 23.2969 μs | 365.25 |    3.00 |    8 | 3.9063 |      - | 108.71 KB |       16.87 |
 'Lambda Include'                     | 2             |     4.550 μs |   0.4434 μs |  0.0243 μs |   0.65 |    0.00 |    1 | 0.2594 |      - |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     4.619 μs |   1.0329 μs |  0.0566 μs |   0.66 |    0.01 |    1 | 0.2594 |      - |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    10.730 μs |   0.2072 μs |  0.0114 μs |   1.54 |    0.00 |    4 | 0.5341 |      - |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    11.601 μs |  13.8836 μs |  0.7610 μs |   1.66 |    0.09 |    4 | 0.5646 |      - |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    29.519 μs |   3.2331 μs |  0.1772 μs |   4.23 |    0.02 |    6 | 1.2207 | 0.0305 |  20.33 KB |        3.15 |
                                      |               |              |             |            |        |         |      |        |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **7.283 μs** |   **2.4508 μs** |  **0.1343 μs** |   **1.00** |    **0.02** |    **3** | **0.4044** |      **-** |   **6.73 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     5.919 μs |   1.3459 μs |  0.0738 μs |   0.81 |    0.02 |    2 | 0.3204 |      - |   5.26 KB |        0.78 |
 'Complex predicates (parameterized)' | 10            |    37.151 μs |   1.9185 μs |  0.1052 μs |   5.10 |    0.08 |    7 | 1.2207 |      - |  20.91 KB |        3.11 |
 'Two criteria (AND)'                 | 10            |     8.431 μs |   2.3099 μs |  0.1266 μs |   1.16 |    0.02 |    3 | 0.3662 |      - |   6.14 KB |        0.91 |
 'Five criteria (AND)'                | 10            |    19.446 μs |   2.5631 μs |  0.1405 μs |   2.67 |    0.05 |    5 | 0.7019 |      - |  11.55 KB |        1.72 |
 'Ten criteria (AND)'                 | 10            |    37.187 μs |   1.7411 μs |  0.0954 μs |   5.11 |    0.08 |    7 | 1.2207 |      - |  20.91 KB |        3.11 |
 'Keyset pagination'                  | 10            |    12.701 μs |   1.3853 μs |  0.0759 μs |   1.74 |    0.03 |    4 | 0.6256 |      - |  10.43 KB |        1.55 |
 'Keyset pagination (fresh cursor)'   | 10            | 2,650.502 μs | 220.2770 μs | 12.0741 μs | 364.00 |    5.93 |    8 | 3.9063 |      - | 109.45 KB |       16.27 |
 'Lambda Include'                     | 10            |     4.703 μs |   0.4164 μs |  0.0228 μs |   0.65 |    0.01 |    1 | 0.2594 |      - |   4.26 KB |        0.63 |
 'String Include'                     | 10            |     4.640 μs |   0.4225 μs |  0.0232 μs |   0.64 |    0.01 |    1 | 0.2594 |      - |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 10            |    10.772 μs |   1.4155 μs |  0.0776 μs |   1.48 |    0.03 |    4 | 0.5341 |      - |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 10            |    11.221 μs |   5.0475 μs |  0.2767 μs |   1.54 |    0.04 |    4 | 0.5646 |      - |   9.36 KB |        1.39 |
 'Full specification (all features)'  | 10            |    29.181 μs |   2.9355 μs |  0.1609 μs |   4.01 |    0.07 |    6 | 1.1902 |      - |  19.88 KB |        2.96 |
