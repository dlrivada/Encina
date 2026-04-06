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
| **&#39;Concurrent RoundRobin&#39;**               | **1**           |  **7.546 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 1           |  7.616 ms |    NA |  1.01 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 1           |  9.104 ms |    NA |  1.21 |                    - |                - |  32.72 KB |       22.28 |
| &#39;Concurrent LeastConnections (lease)&#39; | 1           |  9.608 ms |    NA |  1.27 |                    - |                - |  32.72 KB |       22.28 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **4**           |  **9.211 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 4           |  9.049 ms |    NA |  0.98 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 4           | 10.976 ms |    NA |  1.19 |               3.0000 |           6.0000 |   33.3 KB |       19.20 |
| &#39;Concurrent LeastConnections (lease)&#39; | 4           | 11.560 ms |    NA |  1.26 |               3.0000 |           2.0000 |  33.38 KB |       19.24 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **8**           |  **9.543 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 8           |  9.192 ms |    NA |  0.96 |               2.0000 |                - |   2.11 KB |        1.08 |
| &#39;Concurrent LeastConnections&#39;         | 8           | 11.261 ms |    NA |  1.18 |               5.0000 |          13.0000 |  33.87 KB |       17.34 |
| &#39;Concurrent LeastConnections (lease)&#39; | 8           | 11.448 ms |    NA |  1.20 |               5.0000 |           4.0000 |  33.83 KB |       17.32 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **16**          |  **9.497 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.51 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 16          |  9.354 ms |    NA |  0.98 |               1.0000 |                - |   2.57 KB |        1.02 |
| &#39;Concurrent LeastConnections&#39;         | 16          | 11.159 ms |    NA |  1.17 |               5.0000 |          11.0000 |  34.32 KB |       13.69 |
| &#39;Concurrent LeastConnections (lease)&#39; | 16          | 11.688 ms |    NA |  1.23 |               5.0000 |           3.0000 |  34.34 KB |       13.69 |
