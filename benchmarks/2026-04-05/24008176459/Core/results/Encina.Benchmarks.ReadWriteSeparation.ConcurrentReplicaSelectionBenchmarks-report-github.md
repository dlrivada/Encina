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
| **&#39;Concurrent RoundRobin&#39;**               | **1**           |  **7.725 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 1           |  7.820 ms |    NA |  1.01 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 1           |  9.219 ms |    NA |  1.19 |                    - |                - |  32.72 KB |       22.28 |
| &#39;Concurrent LeastConnections (lease)&#39; | 1           |  9.659 ms |    NA |  1.25 |                    - |                - |  32.72 KB |       22.28 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **4**           |  **9.531 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 4           |  9.400 ms |    NA |  0.99 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 4           | 11.215 ms |    NA |  1.18 |               3.0000 |          10.0000 |  33.38 KB |       19.24 |
| &#39;Concurrent LeastConnections (lease)&#39; | 4           | 12.152 ms |    NA |  1.27 |               3.0000 |           2.0000 |   33.3 KB |       19.20 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **8**           |  **9.612 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 8           |  9.824 ms |    NA |  1.02 |               1.0000 |                - |   1.95 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 8           | 11.335 ms |    NA |  1.18 |               5.0000 |          12.0000 |  33.89 KB |       17.35 |
| &#39;Concurrent LeastConnections (lease)&#39; | 8           | 11.727 ms |    NA |  1.22 |               4.0000 |           2.0000 |  33.67 KB |       17.24 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **16**          |  **9.752 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 16          |  9.583 ms |    NA |  0.98 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 16          | 11.408 ms |    NA |  1.17 |               5.0000 |          12.0000 |  34.32 KB |       14.04 |
| &#39;Concurrent LeastConnections (lease)&#39; | 16          | 11.905 ms |    NA |  1.22 |               5.0000 |           2.0000 |  34.32 KB |       14.04 |
