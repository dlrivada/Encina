
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                               | CriteriaCount | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------- |-------------- |---------:|------:|------:|-----:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             | **379.3 ms** |    **NA** |  **1.00** |    **9** |   **6.63 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             | 363.2 ms |    NA |  0.96 |    3 |   5.45 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             | 373.8 ms |    NA |  0.99 |    7 |   6.29 KB |        0.95 |
 'Two criteria (AND)'                 | 2             | 369.8 ms |    NA |  0.97 |    5 |   6.22 KB |        0.94 |
 'Five criteria (AND)'                | 2             | 378.0 ms |    NA |  1.00 |    8 |  11.63 KB |        1.75 |
 'Ten criteria (AND)'                 | 2             | 399.3 ms |    NA |  1.05 |   12 |  20.91 KB |        3.15 |
 'Keyset pagination'                  | 2             | 382.1 ms |    NA |  1.01 |   10 |  10.62 KB |        1.60 |
 'Keyset pagination (fresh cursor)'   | 2             | 383.1 ms |    NA |  1.01 |   11 | 115.01 KB |       17.34 |
 'Lambda Include'                     | 2             | 355.3 ms |    NA |  0.94 |    2 |   4.34 KB |        0.65 |
 'String Include'                     | 2             | 353.9 ms |    NA |  0.93 |    1 |   4.26 KB |        0.64 |
 'Multi-column ordering'              | 2             | 363.4 ms |    NA |  0.96 |    4 |   8.85 KB |        1.33 |
 'Offset pagination (Skip/Take)'      | 2             | 371.9 ms |    NA |  0.98 |    6 |   9.63 KB |        1.45 |
 'Full specification (all features)'  | 2             | 407.7 ms |    NA |  1.08 |   13 |  20.38 KB |        3.07 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **5**             | **387.9 ms** |    **NA** |  **1.00** |   **11** |   **6.63 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 5             | 368.8 ms |    NA |  0.95 |    4 |   5.45 KB |        0.82 |
 'Complex predicates (parameterized)' | 5             | 382.5 ms |    NA |  0.99 |    9 |  11.63 KB |        1.75 |
 'Two criteria (AND)'                 | 5             | 369.2 ms |    NA |  0.95 |    5 |   6.14 KB |        0.93 |
 'Five criteria (AND)'                | 5             | 377.2 ms |    NA |  0.97 |    7 |  11.55 KB |        1.74 |
 'Ten criteria (AND)'                 | 5             | 397.2 ms |    NA |  1.02 |   12 |  21.14 KB |        3.19 |
 'Keyset pagination'                  | 5             | 384.3 ms |    NA |  0.99 |   10 |  10.77 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 5             | 381.4 ms |    NA |  0.98 |    8 | 139.56 KB |       21.04 |
 'Lambda Include'                     | 5             | 354.9 ms |    NA |  0.91 |    2 |   4.34 KB |        0.65 |
 'String Include'                     | 5             | 352.3 ms |    NA |  0.91 |    1 |   4.33 KB |        0.65 |
 'Multi-column ordering'              | 5             | 361.5 ms |    NA |  0.93 |    3 |   8.77 KB |        1.32 |
 'Offset pagination (Skip/Take)'      | 5             | 373.5 ms |    NA |  0.96 |    6 |   9.62 KB |        1.45 |
 'Full specification (all features)'  | 5             | 409.6 ms |    NA |  1.06 |   13 |  20.38 KB |        3.07 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **10**            | **374.9 ms** |    **NA** |  **1.00** |    **7** |   **6.81 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            | 365.5 ms |    NA |  0.97 |    4 |   5.45 KB |        0.80 |
 'Complex predicates (parameterized)' | 10            | 395.1 ms |    NA |  1.05 |   11 |  21.23 KB |        3.12 |
 'Two criteria (AND)'                 | 10            | 371.6 ms |    NA |  0.99 |    5 |   6.14 KB |        0.90 |
 'Five criteria (AND)'                | 10            | 379.8 ms |    NA |  1.01 |    8 |  11.63 KB |        1.71 |
 'Ten criteria (AND)'                 | 10            | 397.9 ms |    NA |  1.06 |   12 |  21.07 KB |        3.09 |
 'Keyset pagination'                  | 10            | 380.8 ms |    NA |  1.02 |   10 |  10.62 KB |        1.56 |
 'Keyset pagination (fresh cursor)'   | 10            | 380.2 ms |    NA |  1.01 |    9 | 115.59 KB |       16.97 |
 'Lambda Include'                     | 10            | 352.5 ms |    NA |  0.94 |    1 |   4.26 KB |        0.62 |
 'String Include'                     | 10            | 352.6 ms |    NA |  0.94 |    2 |   4.26 KB |        0.62 |
 'Multi-column ordering'              | 10            | 364.0 ms |    NA |  0.97 |    3 |   8.85 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 10            | 373.4 ms |    NA |  1.00 |    6 |    9.7 KB |        1.42 |
 'Full specification (all features)'  | 10            | 415.2 ms |    NA |  1.11 |   13 |   20.6 KB |        3.02 |
