
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                               | CriteriaCount | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------- |-------------- |---------:|------:|------:|-----:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             | **388.9 ms** |    **NA** |  **1.00** |   **10** |    **6.7 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             | 371.0 ms |    NA |  0.95 |    3 |   5.59 KB |        0.83 |
 'Complex predicates (parameterized)' | 2             | 377.3 ms |    NA |  0.97 |    6 |   6.29 KB |        0.94 |
 'Two criteria (AND)'                 | 2             | 376.7 ms |    NA |  0.97 |    5 |   6.22 KB |        0.93 |
 'Five criteria (AND)'                | 2             | 385.3 ms |    NA |  0.99 |    8 |  11.63 KB |        1.74 |
 'Ten criteria (AND)'                 | 2             | 409.4 ms |    NA |  1.05 |   12 |  21.37 KB |        3.19 |
 'Keyset pagination'                  | 2             | 394.8 ms |    NA |  1.02 |   11 |  10.77 KB |        1.61 |
 'Keyset pagination (fresh cursor)'   | 2             | 386.0 ms |    NA |  0.99 |    9 | 114.61 KB |       17.10 |
 'Lambda Include'                     | 2             | 360.2 ms |    NA |  0.93 |    2 |   4.26 KB |        0.64 |
 'String Include'                     | 2             | 356.4 ms |    NA |  0.92 |    1 |   4.34 KB |        0.65 |
 'Multi-column ordering'              | 2             | 373.3 ms |    NA |  0.96 |    4 |   8.99 KB |        1.34 |
 'Offset pagination (Skip/Take)'      | 2             | 381.7 ms |    NA |  0.98 |    7 |   9.63 KB |        1.44 |
 'Full specification (all features)'  | 2             | 416.1 ms |    NA |  1.07 |   13 |  20.07 KB |        2.99 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **5**             | **380.9 ms** |    **NA** |  **1.00** |    **7** |    **6.7 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 5             | 368.3 ms |    NA |  0.97 |    4 |   5.45 KB |        0.81 |
 'Complex predicates (parameterized)' | 5             | 383.3 ms |    NA |  1.01 |    9 |   11.7 KB |        1.74 |
 'Two criteria (AND)'                 | 5             | 375.2 ms |    NA |  0.98 |    5 |   6.21 KB |        0.93 |
 'Five criteria (AND)'                | 5             | 382.9 ms |    NA |  1.01 |    8 |  11.63 KB |        1.73 |
 'Ten criteria (AND)'                 | 5             | 405.6 ms |    NA |  1.06 |   12 |  21.05 KB |        3.14 |
 'Keyset pagination'                  | 5             | 392.4 ms |    NA |  1.03 |   11 |  10.62 KB |        1.58 |
 'Keyset pagination (fresh cursor)'   | 5             | 391.0 ms |    NA |  1.03 |   10 | 139.78 KB |       20.85 |
 'Lambda Include'                     | 5             | 363.6 ms |    NA |  0.95 |    2 |   4.34 KB |        0.65 |
 'String Include'                     | 5             | 356.3 ms |    NA |  0.94 |    1 |   4.26 KB |        0.64 |
 'Multi-column ordering'              | 5             | 367.9 ms |    NA |  0.97 |    3 |   8.77 KB |        1.31 |
 'Offset pagination (Skip/Take)'      | 5             | 375.2 ms |    NA |  0.99 |    6 |   9.55 KB |        1.42 |
 'Full specification (all features)'  | 5             | 417.2 ms |    NA |  1.10 |   13 |  20.21 KB |        3.02 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **10**            | **387.6 ms** |    **NA** |  **1.00** |    **7** |   **6.63 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            | 376.6 ms |    NA |  0.97 |    4 | 125.56 KB |       18.93 |
 'Complex predicates (parameterized)' | 10            | 409.5 ms |    NA |  1.06 |   12 |  20.91 KB |        3.15 |
 'Two criteria (AND)'                 | 10            | 381.7 ms |    NA |  0.98 |    6 |   6.14 KB |        0.93 |
 'Five criteria (AND)'                | 10            | 388.0 ms |    NA |  1.00 |    9 |   11.7 KB |        1.76 |
 'Ten criteria (AND)'                 | 10            | 402.8 ms |    NA |  1.04 |   11 |  21.14 KB |        3.19 |
 'Keyset pagination'                  | 10            | 388.9 ms |    NA |  1.00 |   10 |  10.62 KB |        1.60 |
 'Keyset pagination (fresh cursor)'   | 10            | 387.9 ms |    NA |  1.00 |    8 | 114.37 KB |       17.24 |
 'Lambda Include'                     | 10            | 359.3 ms |    NA |  0.93 |    1 |   4.26 KB |        0.64 |
 'String Include'                     | 10            | 362.0 ms |    NA |  0.93 |    2 |   4.26 KB |        0.64 |
 'Multi-column ordering'              | 10            | 375.1 ms |    NA |  0.97 |    3 |   8.77 KB |        1.32 |
 'Offset pagination (Skip/Take)'      | 10            | 378.6 ms |    NA |  0.98 |    5 |    9.7 KB |        1.46 |
 'Full specification (all features)'  | 10            | 415.3 ms |    NA |  1.07 |   13 |  20.54 KB |        3.10 |
