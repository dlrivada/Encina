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
| **&#39;Concurrent RoundRobin&#39;**               | **1**           |  **7.500 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 1           |  7.529 ms |    NA |  1.00 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 1           |  9.012 ms |    NA |  1.20 |                    - |                - |  32.72 KB |       22.28 |
| &#39;Concurrent LeastConnections (lease)&#39; | 1           |  9.541 ms |    NA |  1.27 |                    - |                - |  32.72 KB |       22.28 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **4**           |  **9.067 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 4           |  8.948 ms |    NA |  0.99 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 4           | 10.700 ms |    NA |  1.18 |               3.0000 |          10.0000 |   33.3 KB |       19.20 |
| &#39;Concurrent LeastConnections (lease)&#39; | 4           | 11.072 ms |    NA |  1.22 |               3.0000 |           2.0000 |  33.38 KB |       19.24 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **8**           |  **9.287 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 8           |  9.116 ms |    NA |  0.98 |               1.0000 |                - |   1.95 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 8           | 10.854 ms |    NA |  1.17 |               5.0000 |          11.0000 |  33.89 KB |       17.35 |
| &#39;Concurrent LeastConnections (lease)&#39; | 8           | 11.436 ms |    NA |  1.23 |               4.0000 |           1.0000 |  33.67 KB |       17.24 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **16**          |  **9.299 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 16          |  9.282 ms |    NA |  1.00 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 16          | 11.034 ms |    NA |  1.19 |               5.0000 |          10.0000 |  34.32 KB |       14.04 |
| &#39;Concurrent LeastConnections (lease)&#39; | 16          | 11.693 ms |    NA |  1.26 |               5.0000 |           2.0000 |  34.32 KB |       14.04 |
