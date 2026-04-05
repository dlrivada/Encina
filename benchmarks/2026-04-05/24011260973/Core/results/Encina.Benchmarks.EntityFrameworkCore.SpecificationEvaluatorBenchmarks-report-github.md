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
| **&#39;Simple Where (single criterion)&#39;**    | **2**             | **379.3 ms** |    **NA** |  **1.00** |    **9** |   **6.63 KB** |        **1.00** |
| &#39;Direct LINQ Where (baseline)&#39;       | 2             | 363.2 ms |    NA |  0.96 |    3 |   5.45 KB |        0.82 |
| &#39;Complex predicates (parameterized)&#39; | 2             | 373.8 ms |    NA |  0.99 |    7 |   6.29 KB |        0.95 |
| &#39;Two criteria (AND)&#39;                 | 2             | 369.8 ms |    NA |  0.97 |    5 |   6.22 KB |        0.94 |
| &#39;Five criteria (AND)&#39;                | 2             | 378.0 ms |    NA |  1.00 |    8 |  11.63 KB |        1.75 |
| &#39;Ten criteria (AND)&#39;                 | 2             | 399.3 ms |    NA |  1.05 |   12 |  20.91 KB |        3.15 |
| &#39;Keyset pagination&#39;                  | 2             | 382.1 ms |    NA |  1.01 |   10 |  10.62 KB |        1.60 |
| &#39;Keyset pagination (fresh cursor)&#39;   | 2             | 383.1 ms |    NA |  1.01 |   11 | 115.01 KB |       17.34 |
| &#39;Lambda Include&#39;                     | 2             | 355.3 ms |    NA |  0.94 |    2 |   4.34 KB |        0.65 |
| &#39;String Include&#39;                     | 2             | 353.9 ms |    NA |  0.93 |    1 |   4.26 KB |        0.64 |
| &#39;Multi-column ordering&#39;              | 2             | 363.4 ms |    NA |  0.96 |    4 |   8.85 KB |        1.33 |
| &#39;Offset pagination (Skip/Take)&#39;      | 2             | 371.9 ms |    NA |  0.98 |    6 |   9.63 KB |        1.45 |
| &#39;Full specification (all features)&#39;  | 2             | 407.7 ms |    NA |  1.08 |   13 |  20.38 KB |        3.07 |
|                                      |               |          |       |       |      |           |             |
| **&#39;Simple Where (single criterion)&#39;**    | **5**             | **387.9 ms** |    **NA** |  **1.00** |   **11** |   **6.63 KB** |        **1.00** |
| &#39;Direct LINQ Where (baseline)&#39;       | 5             | 368.8 ms |    NA |  0.95 |    4 |   5.45 KB |        0.82 |
| &#39;Complex predicates (parameterized)&#39; | 5             | 382.5 ms |    NA |  0.99 |    9 |  11.63 KB |        1.75 |
| &#39;Two criteria (AND)&#39;                 | 5             | 369.2 ms |    NA |  0.95 |    5 |   6.14 KB |        0.93 |
| &#39;Five criteria (AND)&#39;                | 5             | 377.2 ms |    NA |  0.97 |    7 |  11.55 KB |        1.74 |
| &#39;Ten criteria (AND)&#39;                 | 5             | 397.2 ms |    NA |  1.02 |   12 |  21.14 KB |        3.19 |
| &#39;Keyset pagination&#39;                  | 5             | 384.3 ms |    NA |  0.99 |   10 |  10.77 KB |        1.62 |
| &#39;Keyset pagination (fresh cursor)&#39;   | 5             | 381.4 ms |    NA |  0.98 |    8 | 139.56 KB |       21.04 |
| &#39;Lambda Include&#39;                     | 5             | 354.9 ms |    NA |  0.91 |    2 |   4.34 KB |        0.65 |
| &#39;String Include&#39;                     | 5             | 352.3 ms |    NA |  0.91 |    1 |   4.33 KB |        0.65 |
| &#39;Multi-column ordering&#39;              | 5             | 361.5 ms |    NA |  0.93 |    3 |   8.77 KB |        1.32 |
| &#39;Offset pagination (Skip/Take)&#39;      | 5             | 373.5 ms |    NA |  0.96 |    6 |   9.62 KB |        1.45 |
| &#39;Full specification (all features)&#39;  | 5             | 409.6 ms |    NA |  1.06 |   13 |  20.38 KB |        3.07 |
|                                      |               |          |       |       |      |           |             |
| **&#39;Simple Where (single criterion)&#39;**    | **10**            | **374.9 ms** |    **NA** |  **1.00** |    **7** |   **6.81 KB** |        **1.00** |
| &#39;Direct LINQ Where (baseline)&#39;       | 10            | 365.5 ms |    NA |  0.97 |    4 |   5.45 KB |        0.80 |
| &#39;Complex predicates (parameterized)&#39; | 10            | 395.1 ms |    NA |  1.05 |   11 |  21.23 KB |        3.12 |
| &#39;Two criteria (AND)&#39;                 | 10            | 371.6 ms |    NA |  0.99 |    5 |   6.14 KB |        0.90 |
| &#39;Five criteria (AND)&#39;                | 10            | 379.8 ms |    NA |  1.01 |    8 |  11.63 KB |        1.71 |
| &#39;Ten criteria (AND)&#39;                 | 10            | 397.9 ms |    NA |  1.06 |   12 |  21.07 KB |        3.09 |
| &#39;Keyset pagination&#39;                  | 10            | 380.8 ms |    NA |  1.02 |   10 |  10.62 KB |        1.56 |
| &#39;Keyset pagination (fresh cursor)&#39;   | 10            | 380.2 ms |    NA |  1.01 |    9 | 115.59 KB |       16.97 |
| &#39;Lambda Include&#39;                     | 10            | 352.5 ms |    NA |  0.94 |    1 |   4.26 KB |        0.62 |
| &#39;String Include&#39;                     | 10            | 352.6 ms |    NA |  0.94 |    2 |   4.26 KB |        0.62 |
| &#39;Multi-column ordering&#39;              | 10            | 364.0 ms |    NA |  0.97 |    3 |   8.85 KB |        1.30 |
| &#39;Offset pagination (Skip/Take)&#39;      | 10            | 373.4 ms |    NA |  1.00 |    6 |    9.7 KB |        1.42 |
| &#39;Full specification (all features)&#39;  | 10            | 415.2 ms |    NA |  1.11 |   13 |   20.6 KB |        3.02 |
