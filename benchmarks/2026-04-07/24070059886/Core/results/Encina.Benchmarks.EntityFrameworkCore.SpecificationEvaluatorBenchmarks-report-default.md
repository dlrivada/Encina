
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                               | CriteriaCount | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------- |-------------- |---------:|------:|------:|-----:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             | **360.7 ms** |    **NA** |  **1.00** |    **8** |   **6.63 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             | 353.5 ms |    NA |  0.98 |    4 |   5.45 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             | 360.2 ms |    NA |  1.00 |    7 |   6.14 KB |        0.93 |
 'Two criteria (AND)'                 | 2             | 356.3 ms |    NA |  0.99 |    6 |   6.14 KB |        0.93 |
 'Five criteria (AND)'                | 2             | 374.1 ms |    NA |  1.04 |   11 |   11.7 KB |        1.76 |
 'Ten criteria (AND)'                 | 2             | 394.9 ms |    NA |  1.09 |   12 |   21.3 KB |        3.21 |
 'Keyset pagination'                  | 2             | 371.9 ms |    NA |  1.03 |   10 |  10.62 KB |        1.60 |
 'Keyset pagination (fresh cursor)'   | 2             | 368.1 ms |    NA |  1.02 |    9 | 115.45 KB |       17.41 |
 'Lambda Include'                     | 2             | 335.2 ms |    NA |  0.93 |    1 |   4.26 KB |        0.64 |
 'String Include'                     | 2             | 340.0 ms |    NA |  0.94 |    2 |   4.48 KB |        0.67 |
 'Multi-column ordering'              | 2             | 355.7 ms |    NA |  0.99 |    5 |   8.77 KB |        1.32 |
 'Offset pagination (Skip/Take)'      | 2             | 353.4 ms |    NA |  0.98 |    3 |   9.85 KB |        1.49 |
 'Full specification (all features)'  | 2             | 401.4 ms |    NA |  1.11 |   13 |  20.22 KB |        3.05 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **10**            | **369.2 ms** |    **NA** |  **1.00** |    **9** |   **6.74 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            | 362.6 ms |    NA |  0.98 |    6 |   5.45 KB |        0.81 |
 'Complex predicates (parameterized)' | 10            | 391.8 ms |    NA |  1.06 |   11 |  20.91 KB |        3.10 |
 'Two criteria (AND)'                 | 10            | 358.6 ms |    NA |  0.97 |    5 |   6.22 KB |        0.92 |
 'Five criteria (AND)'                | 10            | 364.1 ms |    NA |  0.99 |    7 |  11.63 KB |        1.72 |
 'Ten criteria (AND)'                 | 10            | 393.6 ms |    NA |  1.07 |   12 |  20.91 KB |        3.10 |
 'Keyset pagination'                  | 10            | 364.3 ms |    NA |  0.99 |    8 |  10.62 KB |        1.57 |
 'Keyset pagination (fresh cursor)'   | 10            | 371.4 ms |    NA |  1.01 |   10 |  114.6 KB |       17.00 |
 'Lambda Include'                     | 10            | 333.1 ms |    NA |  0.90 |    1 |   4.26 KB |        0.63 |
 'String Include'                     | 10            | 342.0 ms |    NA |  0.93 |    2 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 10            | 345.5 ms |    NA |  0.94 |    3 |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 10            | 355.0 ms |    NA |  0.96 |    4 |   9.55 KB |        1.42 |
 'Full specification (all features)'  | 10            | 394.6 ms |    NA |  1.07 |   13 |  20.15 KB |        2.99 |
