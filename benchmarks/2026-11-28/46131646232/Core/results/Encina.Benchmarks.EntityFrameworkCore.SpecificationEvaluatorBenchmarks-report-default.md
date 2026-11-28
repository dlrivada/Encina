
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **9.640 μs** |   **0.3243 μs** | **0.0178 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     8.082 μs |   0.1399 μs | 0.0077 μs |   0.84 |    0.00 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    11.104 μs |   0.2379 μs | 0.0130 μs |   1.15 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    11.142 μs |   0.2750 μs | 0.0151 μs |   1.16 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    25.228 μs |   1.1561 μs | 0.0634 μs |   2.62 |    0.01 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    48.129 μs |   4.4267 μs | 0.2426 μs |   4.99 |    0.02 |    5 | 0.2441 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    17.748 μs |   1.6464 μs | 0.0902 μs |   1.84 |    0.01 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,199.004 μs | 140.7921 μs | 7.7173 μs | 331.84 |    0.87 |    6 |      - | 108.79 KB |       16.88 |
 'Lambda Include'                     | 2             |     6.044 μs |   0.0769 μs | 0.0042 μs |   0.63 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     6.031 μs |   0.5276 μs | 0.0289 μs |   0.63 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    14.303 μs |   0.8871 μs | 0.0486 μs |   1.48 |    0.00 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    15.703 μs |   1.9035 μs | 0.1043 μs |   1.63 |    0.01 |    3 | 0.0916 |   9.66 KB |        1.50 |
 'Full specification (all features)'  | 2             |    41.515 μs |   3.8499 μs | 0.2110 μs |   4.31 |    0.02 |    5 | 0.1831 |  19.88 KB |        3.08 |
                                      |               |              |             |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **9.839 μs** |   **1.2217 μs** | **0.0670 μs** |   **1.00** |    **0.01** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     8.482 μs |   0.7110 μs | 0.0390 μs |   0.86 |    0.01 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    48.832 μs |   4.8821 μs | 0.2676 μs |   4.96 |    0.04 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    11.615 μs |   0.3166 μs | 0.0174 μs |   1.18 |    0.01 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    26.076 μs |   0.8699 μs | 0.0477 μs |   2.65 |    0.02 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    48.591 μs |   1.0270 μs | 0.0563 μs |   4.94 |    0.03 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    18.071 μs |   1.2130 μs | 0.0665 μs |   1.84 |    0.01 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,207.566 μs | 182.3830 μs | 9.9970 μs | 326.00 |    2.12 |    6 |      - |    111 KB |       17.22 |
 'Lambda Include'                     | 10            |     6.149 μs |   0.4008 μs | 0.0220 μs |   0.62 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     6.220 μs |   0.9102 μs | 0.0499 μs |   0.63 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    14.454 μs |   1.7890 μs | 0.0981 μs |   1.47 |    0.01 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    15.199 μs |   0.4023 μs | 0.0221 μs |   1.54 |    0.01 |    3 | 0.1068 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    41.958 μs |   2.2260 μs | 0.1220 μs |   4.26 |    0.03 |    5 | 0.2441 |  20.03 KB |        3.11 |
