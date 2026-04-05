
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                               | CriteriaCount | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------- |-------------- |---------:|------:|------:|-----:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             | **380.0 ms** |    **NA** |  **1.00** |    **8** |   **6.75 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             | 362.6 ms |    NA |  0.95 |    3 |   5.45 KB |        0.81 |
 'Complex predicates (parameterized)' | 2             | 368.9 ms |    NA |  0.97 |    4 |   6.14 KB |        0.91 |
 'Two criteria (AND)'                 | 2             | 371.8 ms |    NA |  0.98 |    5 |   6.22 KB |        0.92 |
 'Five criteria (AND)'                | 2             | 382.2 ms |    NA |  1.01 |    9 |  11.71 KB |        1.73 |
 'Ten criteria (AND)'                 | 2             | 402.5 ms |    NA |  1.06 |   12 |  21.07 KB |        3.12 |
 'Keyset pagination'                  | 2             | 383.7 ms |    NA |  1.01 |   10 |  10.77 KB |        1.59 |
 'Keyset pagination (fresh cursor)'   | 2             | 384.1 ms |    NA |  1.01 |   11 | 114.87 KB |       17.02 |
 'Lambda Include'                     | 2             | 355.0 ms |    NA |  0.93 |    1 |   4.26 KB |        0.63 |
 'String Include'                     | 2             | 356.0 ms |    NA |  0.94 |    2 |   4.34 KB |        0.64 |
 'Multi-column ordering'              | 2             | 376.8 ms |    NA |  0.99 |    6 |   8.85 KB |        1.31 |
 'Offset pagination (Skip/Take)'      | 2             | 377.0 ms |    NA |  0.99 |    7 |   9.55 KB |        1.41 |
 'Full specification (all features)'  | 2             | 412.1 ms |    NA |  1.08 |   13 |   20.3 KB |        3.01 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **5**             | **380.2 ms** |    **NA** |  **1.00** |    **7** |    **6.7 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 5             | 365.5 ms |    NA |  0.96 |    3 |   5.45 KB |        0.81 |
 'Complex predicates (parameterized)' | 5             | 387.6 ms |    NA |  1.02 |   10 |  11.63 KB |        1.73 |
 'Two criteria (AND)'                 | 5             | 373.9 ms |    NA |  0.98 |    5 |   6.29 KB |        0.94 |
 'Five criteria (AND)'                | 5             | 384.4 ms |    NA |  1.01 |    9 |  11.55 KB |        1.72 |
 'Ten criteria (AND)'                 | 5             | 399.8 ms |    NA |  1.05 |   12 |   21.2 KB |        3.16 |
 'Keyset pagination'                  | 5             | 382.0 ms |    NA |  1.00 |    8 |  10.69 KB |        1.59 |
 'Keyset pagination (fresh cursor)'   | 5             | 393.7 ms |    NA |  1.04 |   11 | 114.55 KB |       17.09 |
 'Lambda Include'                     | 5             | 354.7 ms |    NA |  0.93 |    2 |   4.26 KB |        0.64 |
 'String Include'                     | 5             | 354.6 ms |    NA |  0.93 |    1 |   4.26 KB |        0.64 |
 'Multi-column ordering'              | 5             | 366.2 ms |    NA |  0.96 |    4 |   8.85 KB |        1.32 |
 'Offset pagination (Skip/Take)'      | 5             | 374.2 ms |    NA |  0.98 |    6 |   9.62 KB |        1.43 |
 'Full specification (all features)'  | 5             | 412.5 ms |    NA |  1.09 |   13 |  20.45 KB |        3.05 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **10**            | **377.7 ms** |    **NA** |  **1.00** |    **7** |   **6.71 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            | 364.0 ms |    NA |  0.96 |    3 |   5.45 KB |        0.81 |
 'Complex predicates (parameterized)' | 10            | 400.2 ms |    NA |  1.06 |   12 |  21.21 KB |        3.16 |
 'Two criteria (AND)'                 | 10            | 374.8 ms |    NA |  0.99 |    5 |   6.14 KB |        0.92 |
 'Five criteria (AND)'                | 10            | 380.5 ms |    NA |  1.01 |    8 |  11.63 KB |        1.73 |
 'Ten criteria (AND)'                 | 10            | 399.6 ms |    NA |  1.06 |   11 |  21.23 KB |        3.16 |
 'Keyset pagination'                  | 10            | 386.4 ms |    NA |  1.02 |   10 |  10.62 KB |        1.58 |
 'Keyset pagination (fresh cursor)'   | 10            | 383.4 ms |    NA |  1.02 |    9 | 138.48 KB |       20.63 |
 'Lambda Include'                     | 10            | 360.1 ms |    NA |  0.95 |    2 |   4.26 KB |        0.63 |
 'String Include'                     | 10            | 358.1 ms |    NA |  0.95 |    1 |   4.33 KB |        0.64 |
 'Multi-column ordering'              | 10            | 366.6 ms |    NA |  0.97 |    4 |   8.77 KB |        1.31 |
 'Offset pagination (Skip/Take)'      | 10            | 377.0 ms |    NA |  1.00 |    6 |   9.55 KB |        1.42 |
 'Full specification (all features)'  | 10            | 420.4 ms |    NA |  1.11 |   13 |  20.38 KB |        3.04 |
