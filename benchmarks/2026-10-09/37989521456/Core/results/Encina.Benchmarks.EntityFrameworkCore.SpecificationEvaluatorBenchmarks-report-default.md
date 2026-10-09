
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.420 μs** |   **0.5376 μs** |  **0.0295 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.152 μs |   0.3064 μs |  0.0168 μs |   0.80 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    13.077 μs |   0.4685 μs |  0.0257 μs |   1.15 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.416 μs |   0.5662 μs |  0.0310 μs |   1.17 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.478 μs |   1.5409 μs |  0.0845 μs |   2.58 |    0.01 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    55.021 μs |   0.6164 μs |  0.0338 μs |   4.82 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    19.868 μs |   0.7848 μs |  0.0430 μs |   1.74 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,213.721 μs | 234.2950 μs | 12.8425 μs | 281.42 |    1.16 |    7 | 3.9063 | 108.97 KB |       16.91 |
 'Lambda Include'                     | 2             |     6.942 μs |   0.3938 μs |  0.0216 μs |   0.61 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.044 μs |   1.6662 μs |  0.0913 μs |   0.62 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    15.612 μs |   1.5735 μs |  0.0862 μs |   1.37 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    16.570 μs |   0.0806 μs |  0.0044 μs |   1.45 |    0.00 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    45.752 μs |   8.8927 μs |  0.4874 μs |   4.01 |    0.04 |    5 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.086 μs** |   **0.4112 μs** |  **0.0225 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.343 μs |   0.3119 μs |  0.0171 μs |   0.84 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    54.927 μs |   4.3923 μs |  0.2408 μs |   4.95 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.098 μs |   0.9179 μs |  0.0503 μs |   1.18 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.326 μs |   1.5234 μs |  0.0835 μs |   2.65 |    0.01 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    55.168 μs |   2.2314 μs |  0.1223 μs |   4.98 |    0.01 |    6 | 1.2817 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 10            |    20.047 μs |   0.5369 μs |  0.0294 μs |   1.81 |    0.00 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,223.952 μs |  59.3714 μs |  3.2543 μs | 290.81 |    0.57 |    7 | 3.9063 | 108.99 KB |       16.91 |
 'Lambda Include'                     | 10            |     6.874 μs |   0.2380 μs |  0.0130 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.017 μs |   0.4795 μs |  0.0263 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.043 μs |   0.4672 μs |  0.0256 μs |   1.45 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    16.736 μs |   1.0615 μs |  0.0582 μs |   1.51 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    45.288 μs |   1.7713 μs |  0.0971 μs |   4.09 |    0.01 |    5 | 1.1597 |  19.88 KB |        3.08 |
