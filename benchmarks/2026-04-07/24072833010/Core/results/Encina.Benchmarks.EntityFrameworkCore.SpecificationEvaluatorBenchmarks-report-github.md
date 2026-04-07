```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.40GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                               | CriteriaCount | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------- |-------------- |---------:|------:|------:|-----:|----------:|------------:|
| **&#39;Simple Where (single criterion)&#39;**    | **2**             | **390.2 ms** |    **NA** |  **1.00** |    **8** |   **6.71 KB** |        **1.00** |
| &#39;Direct LINQ Where (baseline)&#39;       | 2             | 376.8 ms |    NA |  0.97 |    4 |   5.52 KB |        0.82 |
| &#39;Complex predicates (parameterized)&#39; | 2             | 385.6 ms |    NA |  0.99 |    5 |   6.21 KB |        0.93 |
| &#39;Two criteria (AND)&#39;                 | 2             | 389.5 ms |    NA |  1.00 |    7 |   6.21 KB |        0.93 |
| &#39;Five criteria (AND)&#39;                | 2             | 396.3 ms |    NA |  1.02 |   11 |  11.63 KB |        1.73 |
| &#39;Ten criteria (AND)&#39;                 | 2             | 401.4 ms |    NA |  1.03 |   12 |  20.98 KB |        3.13 |
| &#39;Keyset pagination&#39;                  | 2             | 394.0 ms |    NA |  1.01 |   10 |  10.62 KB |        1.58 |
| &#39;Keyset pagination (fresh cursor)&#39;   | 2             | 393.5 ms |    NA |  1.01 |    9 | 114.46 KB |       17.06 |
| &#39;Lambda Include&#39;                     | 2             | 360.7 ms |    NA |  0.92 |    2 |   4.33 KB |        0.64 |
| &#39;String Include&#39;                     | 2             | 359.5 ms |    NA |  0.92 |    1 |   4.26 KB |        0.63 |
| &#39;Multi-column ordering&#39;              | 2             | 372.7 ms |    NA |  0.96 |    3 |   8.85 KB |        1.32 |
| &#39;Offset pagination (Skip/Take)&#39;      | 2             | 388.0 ms |    NA |  0.99 |    6 |   9.55 KB |        1.42 |
| &#39;Full specification (all features)&#39;  | 2             | 420.8 ms |    NA |  1.08 |   13 |  20.53 KB |        3.06 |
|                                      |               |          |       |       |      |           |             |
| **&#39;Simple Where (single criterion)&#39;**    | **10**            | **381.3 ms** |    **NA** |  **1.00** |    **6** |      **7 KB** |        **1.00** |
| &#39;Direct LINQ Where (baseline)&#39;       | 10            | 371.0 ms |    NA |  0.97 |    3 |   5.45 KB |        0.78 |
| &#39;Complex predicates (parameterized)&#39; | 10            | 425.5 ms |    NA |  1.12 |   13 |  20.99 KB |        3.00 |
| &#39;Two criteria (AND)&#39;                 | 10            | 372.2 ms |    NA |  0.98 |    4 |   6.14 KB |        0.88 |
| &#39;Five criteria (AND)&#39;                | 10            | 388.2 ms |    NA |  1.02 |    8 |  11.55 KB |        1.65 |
| &#39;Ten criteria (AND)&#39;                 | 10            | 416.1 ms |    NA |  1.09 |   11 |  21.07 KB |        3.01 |
| &#39;Keyset pagination&#39;                  | 10            | 390.3 ms |    NA |  1.02 |    9 |  10.62 KB |        1.52 |
| &#39;Keyset pagination (fresh cursor)&#39;   | 10            | 393.4 ms |    NA |  1.03 |   10 | 114.59 KB |       16.37 |
| &#39;Lambda Include&#39;                     | 10            | 365.7 ms |    NA |  0.96 |    2 |   4.26 KB |        0.61 |
| &#39;String Include&#39;                     | 10            | 363.3 ms |    NA |  0.95 |    1 |   4.26 KB |        0.61 |
| &#39;Multi-column ordering&#39;              | 10            | 378.5 ms |    NA |  0.99 |    5 |   8.77 KB |        1.25 |
| &#39;Offset pagination (Skip/Take)&#39;      | 10            | 383.7 ms |    NA |  1.01 |    7 |    9.7 KB |        1.39 |
| &#39;Full specification (all features)&#39;  | 10            | 423.1 ms |    NA |  1.11 |   12 |  20.23 KB |        2.89 |
