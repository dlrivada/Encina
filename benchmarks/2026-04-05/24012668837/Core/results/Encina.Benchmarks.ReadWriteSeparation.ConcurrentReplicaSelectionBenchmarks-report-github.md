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
| **&#39;Concurrent RoundRobin&#39;**               | **1**           |  **7.602 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 1           |  7.509 ms |    NA |  0.99 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 1           |  9.312 ms |    NA |  1.23 |                    - |                - |  32.72 KB |       22.28 |
| &#39;Concurrent LeastConnections (lease)&#39; | 1           |  9.902 ms |    NA |  1.30 |                    - |                - |  32.72 KB |       22.28 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **4**           |  **9.695 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 4           |  9.936 ms |    NA |  1.02 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 4           | 11.911 ms |    NA |  1.23 |               3.0000 |          12.0000 |   33.3 KB |       19.20 |
| &#39;Concurrent LeastConnections (lease)&#39; | 4           | 12.164 ms |    NA |  1.25 |               3.0000 |           4.0000 |  33.38 KB |       19.24 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **8**           | **10.445 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 8           |  9.623 ms |    NA |  0.92 |               1.0000 |                - |   1.95 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39;         | 8           | 11.238 ms |    NA |  1.08 |               5.0000 |           7.0000 |  33.83 KB |       17.32 |
| &#39;Concurrent LeastConnections (lease)&#39; | 8           | 11.871 ms |    NA |  1.14 |               5.0000 |           3.0000 |  33.95 KB |       17.38 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**               | **16**          |  **9.520 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.48 KB** |        **1.00** |
| &#39;Concurrent Random&#39;                   | 16          |  9.835 ms |    NA |  1.03 |               1.0000 |                - |   2.45 KB |        0.98 |
| &#39;Concurrent LeastConnections&#39;         | 16          | 11.594 ms |    NA |  1.22 |               5.0000 |          14.0000 |  34.38 KB |       13.84 |
| &#39;Concurrent LeastConnections (lease)&#39; | 16          | 11.901 ms |    NA |  1.25 |               5.0000 |           3.0000 |  34.32 KB |       13.81 |
