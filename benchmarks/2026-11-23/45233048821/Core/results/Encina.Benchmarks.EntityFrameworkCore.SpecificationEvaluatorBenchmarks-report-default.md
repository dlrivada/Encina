
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **10.094 μs** |   **0.6133 μs** |  **0.0336 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     8.504 μs |   1.5853 μs |  0.0869 μs |   0.84 |    0.01 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    11.390 μs |   0.6452 μs |  0.0354 μs |   1.13 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    11.481 μs |   0.9165 μs |  0.0502 μs |   1.14 |    0.01 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    26.011 μs |   0.5910 μs |  0.0324 μs |   2.58 |    0.01 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    48.758 μs |   1.1779 μs |  0.0646 μs |   4.83 |    0.01 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    18.101 μs |   1.0230 μs |  0.0561 μs |   1.79 |    0.01 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,176.097 μs | 270.4048 μs | 14.8218 μs | 314.64 |    1.56 |    6 |      - | 110.31 KB |       17.11 |
 'Lambda Include'                     | 2             |     6.299 μs |   0.1268 μs |  0.0069 μs |   0.62 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     6.332 μs |   0.1476 μs |  0.0081 μs |   0.63 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    14.756 μs |   0.3632 μs |  0.0199 μs |   1.46 |    0.00 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    16.061 μs |   0.4434 μs |  0.0243 μs |   1.59 |    0.01 |    3 | 0.0916 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    42.012 μs |   3.9126 μs |  0.2145 μs |   4.16 |    0.02 |    5 | 0.1831 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **10.043 μs** |   **0.3960 μs** |  **0.0217 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     8.604 μs |   0.4265 μs |  0.0234 μs |   0.86 |    0.00 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    49.215 μs |   1.6249 μs |  0.0891 μs |   4.90 |    0.01 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    11.484 μs |   0.3624 μs |  0.0199 μs |   1.14 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    26.470 μs |   0.8631 μs |  0.0473 μs |   2.64 |    0.01 |    4 | 0.1221 |  11.71 KB |        1.82 |
 'Ten criteria (AND)'                 | 10            |    48.821 μs |   1.3976 μs |  0.0766 μs |   4.86 |    0.01 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    17.987 μs |   0.2139 μs |  0.0117 μs |   1.79 |    0.00 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,195.898 μs | 304.0660 μs | 16.6669 μs | 318.21 |    1.56 |    6 |      - | 110.53 KB |       17.15 |
 'Lambda Include'                     | 10            |     6.229 μs |   0.4785 μs |  0.0262 μs |   0.62 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     6.146 μs |   0.1516 μs |  0.0083 μs |   0.61 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    14.610 μs |   0.4789 μs |  0.0263 μs |   1.45 |    0.00 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    15.548 μs |   1.4577 μs |  0.0799 μs |   1.55 |    0.01 |    3 | 0.0916 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    43.337 μs |   1.5999 μs |  0.0877 μs |   4.32 |    0.01 |    5 | 0.2441 |  20.04 KB |        3.11 |
