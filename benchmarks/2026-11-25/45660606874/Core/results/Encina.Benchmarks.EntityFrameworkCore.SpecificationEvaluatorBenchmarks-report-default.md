
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **9.855 μs** |   **0.6311 μs** |  **0.0346 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     8.592 μs |   0.4075 μs |  0.0223 μs |   0.87 |    0.00 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    11.473 μs |   0.3782 μs |  0.0207 μs |   1.16 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    11.597 μs |   0.5438 μs |  0.0298 μs |   1.18 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    26.194 μs |   0.8831 μs |  0.0484 μs |   2.66 |    0.01 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    48.031 μs |   1.3970 μs |  0.0766 μs |   4.87 |    0.02 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    17.867 μs |   0.7378 μs |  0.0404 μs |   1.81 |    0.01 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,299.007 μs | 336.4372 μs | 18.4413 μs | 334.74 |    1.91 |    6 |      - |  110.2 KB |       17.10 |
 'Lambda Include'                     | 2             |     6.249 μs |   1.1150 μs |  0.0611 μs |   0.63 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     6.364 μs |   0.2133 μs |  0.0117 μs |   0.65 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    14.812 μs |   0.7237 μs |  0.0397 μs |   1.50 |    0.01 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    15.447 μs |   0.9459 μs |  0.0518 μs |   1.57 |    0.01 |    3 | 0.0916 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    41.859 μs |   3.0950 μs |  0.1696 μs |   4.25 |    0.02 |    5 | 0.1831 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **10.217 μs** |   **0.6469 μs** |  **0.0355 μs** |   **1.00** |    **0.00** |    **3** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     8.435 μs |   0.4888 μs |  0.0268 μs |   0.83 |    0.00 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    48.729 μs |   0.0288 μs |  0.0016 μs |   4.77 |    0.01 |    6 | 0.2441 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    11.521 μs |   0.5186 μs |  0.0284 μs |   1.13 |    0.00 |    3 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    26.221 μs |   0.3915 μs |  0.0215 μs |   2.57 |    0.01 |    5 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    48.680 μs |   0.3097 μs |  0.0170 μs |   4.76 |    0.01 |    6 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    18.045 μs |   0.5977 μs |  0.0328 μs |   1.77 |    0.01 |    4 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,261.581 μs | 245.1594 μs | 13.4380 μs | 319.24 |    1.49 |    7 |      - | 109.63 KB |       17.01 |
 'Lambda Include'                     | 10            |     6.321 μs |   0.0453 μs |  0.0025 μs |   0.62 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     6.316 μs |   0.1454 μs |  0.0080 μs |   0.62 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    14.605 μs |   0.5768 μs |  0.0316 μs |   1.43 |    0.01 |    4 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    15.750 μs |   0.7644 μs |  0.0419 μs |   1.54 |    0.01 |    4 | 0.0916 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    42.976 μs |   3.9474 μs |  0.2164 μs |   4.21 |    0.02 |    6 | 0.2441 |  20.04 KB |        3.11 |
