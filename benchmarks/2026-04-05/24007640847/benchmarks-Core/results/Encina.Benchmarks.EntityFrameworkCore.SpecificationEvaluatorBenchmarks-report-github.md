```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                               | CriteriaCount | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------- |-------------- |---------:|------:|------:|-----:|----------:|------------:|
| **&#39;Simple Where (single criterion)&#39;**    | **2**             | **388.9 ms** |    **NA** |  **1.00** |   **10** |    **6.7 KB** |        **1.00** |
| &#39;Direct LINQ Where (baseline)&#39;       | 2             | 371.0 ms |    NA |  0.95 |    3 |   5.59 KB |        0.83 |
| &#39;Complex predicates (parameterized)&#39; | 2             | 377.3 ms |    NA |  0.97 |    6 |   6.29 KB |        0.94 |
| &#39;Two criteria (AND)&#39;                 | 2             | 376.7 ms |    NA |  0.97 |    5 |   6.22 KB |        0.93 |
| &#39;Five criteria (AND)&#39;                | 2             | 385.3 ms |    NA |  0.99 |    8 |  11.63 KB |        1.74 |
| &#39;Ten criteria (AND)&#39;                 | 2             | 409.4 ms |    NA |  1.05 |   12 |  21.37 KB |        3.19 |
| &#39;Keyset pagination&#39;                  | 2             | 394.8 ms |    NA |  1.02 |   11 |  10.77 KB |        1.61 |
| &#39;Keyset pagination (fresh cursor)&#39;   | 2             | 386.0 ms |    NA |  0.99 |    9 | 114.61 KB |       17.10 |
| &#39;Lambda Include&#39;                     | 2             | 360.2 ms |    NA |  0.93 |    2 |   4.26 KB |        0.64 |
| &#39;String Include&#39;                     | 2             | 356.4 ms |    NA |  0.92 |    1 |   4.34 KB |        0.65 |
| &#39;Multi-column ordering&#39;              | 2             | 373.3 ms |    NA |  0.96 |    4 |   8.99 KB |        1.34 |
| &#39;Offset pagination (Skip/Take)&#39;      | 2             | 381.7 ms |    NA |  0.98 |    7 |   9.63 KB |        1.44 |
| &#39;Full specification (all features)&#39;  | 2             | 416.1 ms |    NA |  1.07 |   13 |  20.07 KB |        2.99 |
|                                      |               |          |       |       |      |           |             |
| **&#39;Simple Where (single criterion)&#39;**    | **5**             | **380.9 ms** |    **NA** |  **1.00** |    **7** |    **6.7 KB** |        **1.00** |
| &#39;Direct LINQ Where (baseline)&#39;       | 5             | 368.3 ms |    NA |  0.97 |    4 |   5.45 KB |        0.81 |
| &#39;Complex predicates (parameterized)&#39; | 5             | 383.3 ms |    NA |  1.01 |    9 |   11.7 KB |        1.74 |
| &#39;Two criteria (AND)&#39;                 | 5             | 375.2 ms |    NA |  0.98 |    5 |   6.21 KB |        0.93 |
| &#39;Five criteria (AND)&#39;                | 5             | 382.9 ms |    NA |  1.01 |    8 |  11.63 KB |        1.73 |
| &#39;Ten criteria (AND)&#39;                 | 5             | 405.6 ms |    NA |  1.06 |   12 |  21.05 KB |        3.14 |
| &#39;Keyset pagination&#39;                  | 5             | 392.4 ms |    NA |  1.03 |   11 |  10.62 KB |        1.58 |
| &#39;Keyset pagination (fresh cursor)&#39;   | 5             | 391.0 ms |    NA |  1.03 |   10 | 139.78 KB |       20.85 |
| &#39;Lambda Include&#39;                     | 5             | 363.6 ms |    NA |  0.95 |    2 |   4.34 KB |        0.65 |
| &#39;String Include&#39;                     | 5             | 356.3 ms |    NA |  0.94 |    1 |   4.26 KB |        0.64 |
| &#39;Multi-column ordering&#39;              | 5             | 367.9 ms |    NA |  0.97 |    3 |   8.77 KB |        1.31 |
| &#39;Offset pagination (Skip/Take)&#39;      | 5             | 375.2 ms |    NA |  0.99 |    6 |   9.55 KB |        1.42 |
| &#39;Full specification (all features)&#39;  | 5             | 417.2 ms |    NA |  1.10 |   13 |  20.21 KB |        3.02 |
|                                      |               |          |       |       |      |           |             |
| **&#39;Simple Where (single criterion)&#39;**    | **10**            | **387.6 ms** |    **NA** |  **1.00** |    **7** |   **6.63 KB** |        **1.00** |
| &#39;Direct LINQ Where (baseline)&#39;       | 10            | 376.6 ms |    NA |  0.97 |    4 | 125.56 KB |       18.93 |
| &#39;Complex predicates (parameterized)&#39; | 10            | 409.5 ms |    NA |  1.06 |   12 |  20.91 KB |        3.15 |
| &#39;Two criteria (AND)&#39;                 | 10            | 381.7 ms |    NA |  0.98 |    6 |   6.14 KB |        0.93 |
| &#39;Five criteria (AND)&#39;                | 10            | 388.0 ms |    NA |  1.00 |    9 |   11.7 KB |        1.76 |
| &#39;Ten criteria (AND)&#39;                 | 10            | 402.8 ms |    NA |  1.04 |   11 |  21.14 KB |        3.19 |
| &#39;Keyset pagination&#39;                  | 10            | 388.9 ms |    NA |  1.00 |   10 |  10.62 KB |        1.60 |
| &#39;Keyset pagination (fresh cursor)&#39;   | 10            | 387.9 ms |    NA |  1.00 |    8 | 114.37 KB |       17.24 |
| &#39;Lambda Include&#39;                     | 10            | 359.3 ms |    NA |  0.93 |    1 |   4.26 KB |        0.64 |
| &#39;String Include&#39;                     | 10            | 362.0 ms |    NA |  0.93 |    2 |   4.26 KB |        0.64 |
| &#39;Multi-column ordering&#39;              | 10            | 375.1 ms |    NA |  0.97 |    3 |   8.77 KB |        1.32 |
| &#39;Offset pagination (Skip/Take)&#39;      | 10            | 378.6 ms |    NA |  0.98 |    5 |    9.7 KB |        1.46 |
| &#39;Full specification (all features)&#39;  | 10            | 415.3 ms |    NA |  1.07 |   13 |  20.54 KB |        3.10 |
