
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                               | CriteriaCount | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------- |-------------- |---------:|------:|------:|-----:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             | **387.5 ms** |    **NA** |  **1.00** |    **7** |   **6.63 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             | 374.9 ms |    NA |  0.97 |    3 |   5.45 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             | 382.4 ms |    NA |  0.99 |    5 |   6.21 KB |        0.94 |
 'Two criteria (AND)'                 | 2             | 391.6 ms |    NA |  1.01 |    9 |   6.14 KB |        0.93 |
 'Five criteria (AND)'                | 2             | 391.6 ms |    NA |  1.01 |    8 |  11.63 KB |        1.75 |
 'Ten criteria (AND)'                 | 2             | 410.1 ms |    NA |  1.06 |   12 |  21.14 KB |        3.19 |
 'Keyset pagination'                  | 2             | 395.5 ms |    NA |  1.02 |   10 | 106.74 KB |       16.09 |
 'Keyset pagination (fresh cursor)'   | 2             | 400.7 ms |    NA |  1.03 |   11 | 114.33 KB |       17.24 |
 'Lambda Include'                     | 2             | 372.4 ms |    NA |  0.96 |    2 |   4.34 KB |        0.65 |
 'String Include'                     | 2             | 369.2 ms |    NA |  0.95 |    1 |   4.33 KB |        0.65 |
 'Multi-column ordering'              | 2             | 377.6 ms |    NA |  0.97 |    4 |   8.77 KB |        1.32 |
 'Offset pagination (Skip/Take)'      | 2             | 383.7 ms |    NA |  0.99 |    6 |   9.62 KB |        1.45 |
 'Full specification (all features)'  | 2             | 434.7 ms |    NA |  1.12 |   13 |   20.6 KB |        3.11 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **5**             | **387.9 ms** |    **NA** |  **1.00** |    **6** |   **6.71 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 5             | 380.2 ms |    NA |  0.98 |    3 |   5.48 KB |        0.82 |
 'Complex predicates (parameterized)' | 5             | 401.2 ms |    NA |  1.03 |   11 |   11.7 KB |        1.74 |
 'Two criteria (AND)'                 | 5             | 381.6 ms |    NA |  0.98 |    4 |   6.14 KB |        0.92 |
 'Five criteria (AND)'                | 5             | 400.9 ms |    NA |  1.03 |   10 |  11.63 KB |        1.73 |
 'Ten criteria (AND)'                 | 5             | 413.3 ms |    NA |  1.07 |   12 |  20.99 KB |        3.13 |
 'Keyset pagination'                  | 5             | 394.4 ms |    NA |  1.02 |    9 |  10.69 KB |        1.59 |
 'Keyset pagination (fresh cursor)'   | 5             | 391.7 ms |    NA |  1.01 |    8 | 114.86 KB |       17.12 |
 'Lambda Include'                     | 5             | 364.7 ms |    NA |  0.94 |    1 |   4.26 KB |        0.63 |
 'String Include'                     | 5             | 368.2 ms |    NA |  0.95 |    2 |   4.33 KB |        0.64 |
 'Multi-column ordering'              | 5             | 381.8 ms |    NA |  0.98 |    5 |   8.77 KB |        1.31 |
 'Offset pagination (Skip/Take)'      | 5             | 388.5 ms |    NA |  1.00 |    7 |    9.7 KB |        1.45 |
 'Full specification (all features)'  | 5             | 432.7 ms |    NA |  1.12 |   13 |  20.23 KB |        3.01 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **10**            | **386.4 ms** |    **NA** |  **1.00** |    **6** |   **6.71 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            | 379.3 ms |    NA |  0.98 |    4 |   5.45 KB |        0.81 |
 'Complex predicates (parameterized)' | 10            | 420.0 ms |    NA |  1.09 |   12 |  20.98 KB |        3.13 |
 'Two criteria (AND)'                 | 10            | 384.8 ms |    NA |  1.00 |    5 |   6.14 KB |        0.92 |
 'Five criteria (AND)'                | 10            | 393.5 ms |    NA |  1.02 |    9 |  11.77 KB |        1.75 |
 'Ten criteria (AND)'                 | 10            | 405.1 ms |    NA |  1.05 |   11 |  21.07 KB |        3.14 |
 'Keyset pagination'                  | 10            | 392.8 ms |    NA |  1.02 |    8 |   10.7 KB |        1.59 |
 'Keyset pagination (fresh cursor)'   | 10            | 397.1 ms |    NA |  1.03 |   10 | 113.98 KB |       16.98 |
 'Lambda Include'                     | 10            | 364.4 ms |    NA |  0.94 |    1 |   4.26 KB |        0.63 |
 'String Include'                     | 10            | 366.2 ms |    NA |  0.95 |    2 |   4.33 KB |        0.64 |
 'Multi-column ordering'              | 10            | 373.2 ms |    NA |  0.97 |    3 |   8.77 KB |        1.31 |
 'Offset pagination (Skip/Take)'      | 10            | 389.8 ms |    NA |  1.01 |    7 |   9.63 KB |        1.43 |
 'Full specification (all features)'  | 10            | 423.3 ms |    NA |  1.10 |   13 |   20.3 KB |        3.02 |
