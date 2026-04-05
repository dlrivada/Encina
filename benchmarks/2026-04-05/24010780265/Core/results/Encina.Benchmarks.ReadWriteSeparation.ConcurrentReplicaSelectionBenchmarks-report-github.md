```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                | ThreadCount | Mean      | Error | Ratio | Completed Work Items | Lock Contentions | Allocated | Alloc Ratio |
|-------------------------------------- |------------ |----------:|------:|------:|---------------------:|-----------------:|----------:|------------:|
| **&#39;Concurrent RoundRobin&#39;**               | **1**           |  **7.448 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 1           |  7.683 ms |    NA |  1.03 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 1           |  9.080 ms |    NA |  1.22 |                    - |                - |  32.72 KB |       22.28 |
| &#39;Concurrent LeastConnections (lease)&#39; | 1           |  9.369 ms |    NA |  1.26 |                    - |                - |  32.72 KB |       22.28 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **4**           |  **8.930 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 4           |  8.939 ms |    NA |  1.00 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 4           | 10.757 ms |    NA |  1.20 |               3.0000 |           7.0000 |  33.38 KB |       19.24 |
| &#39;Concurrent LeastConnections (lease)&#39; | 4           | 11.062 ms |    NA |  1.24 |               3.0000 |           2.0000 |  33.38 KB |       19.24 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **8**           |  **9.181 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 8           |  9.019 ms |    NA |  0.98 |               1.0000 |                - |   1.95 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 8           | 10.773 ms |    NA |  1.17 |               5.0000 |          11.0000 |  33.89 KB |       17.35 |
| &#39;Concurrent LeastConnections (lease)&#39; | 8           | 11.856 ms |    NA |  1.29 |               5.0000 |           3.0000 |  33.83 KB |       17.32 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **16**          |  **9.072 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 16          |  8.984 ms |    NA |  0.99 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 16          | 10.986 ms |    NA |  1.21 |               5.0000 |          12.0000 |  34.41 KB |       14.07 |
| &#39;Concurrent LeastConnections (lease)&#39; | 16          | 11.278 ms |    NA |  1.24 |               5.0000 |           5.0000 |  34.45 KB |       14.09 |
