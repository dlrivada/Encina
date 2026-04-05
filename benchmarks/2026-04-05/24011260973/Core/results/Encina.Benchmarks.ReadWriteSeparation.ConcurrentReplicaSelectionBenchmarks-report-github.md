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
| **&#39;Concurrent RoundRobin&#39;**               | **1**           |  **7.385 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 1           |  7.584 ms |    NA |  1.03 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 1           |  8.902 ms |    NA |  1.21 |                    - |                - |  32.72 KB |       22.28 |
| &#39;Concurrent LeastConnections (lease)&#39; | 1           |  9.441 ms |    NA |  1.28 |                    - |                - |  32.72 KB |       22.28 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **4**           |  **8.914 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 4           |  8.787 ms |    NA |  0.99 |               2.0000 |                - |   1.89 KB |        1.09 |
| &#39;Concurrent LeastConnections&#39;         | 4           | 10.602 ms |    NA |  1.19 |               3.0000 |          13.0000 |   33.3 KB |       19.20 |
| &#39;Concurrent LeastConnections (lease)&#39; | 4           | 11.150 ms |    NA |  1.25 |               3.0000 |           1.0000 |  33.38 KB |       19.24 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **8**           |  **9.116 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 8           |  8.922 ms |    NA |  0.98 |               1.0000 |                - |   1.95 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 8           | 10.710 ms |    NA |  1.17 |               5.0000 |          13.0000 |  33.83 KB |       17.32 |
| &#39;Concurrent LeastConnections (lease)&#39; | 8           | 11.367 ms |    NA |  1.25 |               5.0000 |           2.0000 |  33.89 KB |       17.35 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **16**          |  **9.108 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 16          |  9.271 ms |    NA |  1.02 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 16          | 10.799 ms |    NA |  1.19 |               5.0000 |           5.0000 |  34.32 KB |       14.04 |
| &#39;Concurrent LeastConnections (lease)&#39; | 16          | 11.359 ms |    NA |  1.25 |               5.0000 |           3.0000 |  34.36 KB |       14.05 |
