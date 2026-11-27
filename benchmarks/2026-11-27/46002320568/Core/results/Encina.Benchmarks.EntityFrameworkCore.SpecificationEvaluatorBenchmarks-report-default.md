
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **14.607 μs** |   **0.4297 μs** |  **0.0236 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    12.064 μs |   0.1778 μs |  0.0097 μs |   0.83 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    17.023 μs |   2.1242 μs |  0.1164 μs |   1.17 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    17.371 μs |   1.4371 μs |  0.0788 μs |   1.19 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    37.788 μs |   1.6833 μs |  0.0923 μs |   2.59 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    70.615 μs |   2.5015 μs |  0.1371 μs |   4.83 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    26.693 μs |   1.8002 μs |  0.0987 μs |   1.83 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,134.776 μs | 933.7973 μs | 51.1846 μs | 283.07 |    3.06 |    7 |      - | 109.15 KB |       16.93 |
 'Lambda Include'                     | 2             |     9.117 μs |   0.3312 μs |  0.0182 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     9.338 μs |   2.4212 μs |  0.1327 μs |   0.64 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    20.732 μs |   1.0333 μs |  0.0566 μs |   1.42 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    21.771 μs |   1.4507 μs |  0.0795 μs |   1.49 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    61.264 μs |   7.1979 μs |  0.3945 μs |   4.19 |    0.02 |    6 | 1.2207 |  20.34 KB |        3.16 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **15.229 μs** |   **0.2389 μs** |  **0.0131 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    12.004 μs |   1.2837 μs |  0.0704 μs |   0.79 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    72.608 μs |   4.3167 μs |  0.2366 μs |   4.77 |    0.01 |    7 | 1.2207 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    17.033 μs |   1.5830 μs |  0.0868 μs |   1.12 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    37.901 μs |   0.9729 μs |  0.0533 μs |   2.49 |    0.00 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    71.139 μs |   1.5796 μs |  0.0866 μs |   4.67 |    0.01 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    26.387 μs |   2.4839 μs |  0.1361 μs |   1.73 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,113.334 μs | 471.2228 μs | 25.8293 μs | 270.10 |    1.48 |    8 |      - | 108.89 KB |       16.89 |
 'Lambda Include'                     | 10            |     9.246 μs |   0.8935 μs |  0.0490 μs |   0.61 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     9.258 μs |   0.9107 μs |  0.0499 μs |   0.61 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    20.874 μs |   1.6297 μs |  0.0893 μs |   1.37 |    0.01 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    22.730 μs |   1.7842 μs |  0.0978 μs |   1.49 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    58.963 μs |   1.2552 μs |  0.0688 μs |   3.87 |    0.00 |    6 | 1.1597 |  19.88 KB |        3.08 |
