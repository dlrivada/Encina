```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                        | ThreadCount | Mean      | Error | Ratio | Completed Work Items | Lock Contentions | Allocated | Alloc Ratio |
|------------------------------ |------------ |----------:|------:|------:|---------------------:|-----------------:|----------:|------------:|
| **&#39;Concurrent RoundRobin&#39;**       | **1**           |  **7.470 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 1           |  7.473 ms |    NA |  1.00 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 1           |  8.321 ms |    NA |  1.11 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 1           |  8.349 ms |    NA |  1.12 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 1           |  8.063 ms |    NA |  1.08 |                    - |                - |   1.47 KB |        1.00 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **4**           |  **9.037 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 4           |  9.381 ms |    NA |  1.04 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 4           |  9.840 ms |    NA |  1.09 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent LeastConnections&#39; | 4           | 10.119 ms |    NA |  1.12 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent WeightedRandom&#39;   | 4           |  9.473 ms |    NA |  1.05 |               3.0000 |                - |   2.05 KB |        1.18 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **8**           |  **9.319 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 8           |  9.130 ms |    NA |  0.98 |               2.0000 |                - |   2.11 KB |        1.08 |
| &#39;Concurrent LeastLatency&#39;     | 8           | 11.022 ms |    NA |  1.18 |               4.0000 |                - |   2.42 KB |        1.24 |
| &#39;Concurrent LeastConnections&#39; | 8           | 10.494 ms |    NA |  1.13 |               4.0000 |                - |   2.45 KB |        1.25 |
| &#39;Concurrent WeightedRandom&#39;   | 8           |  9.672 ms |    NA |  1.04 |               3.0000 |                - |   2.29 KB |        1.17 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **16**          |  **9.381 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 16          |  9.259 ms |    NA |  0.99 |               2.0000 |                - |   2.63 KB |        1.07 |
| &#39;Concurrent LeastLatency&#39;     | 16          | 10.125 ms |    NA |  1.08 |               4.0000 |                - |   2.94 KB |        1.20 |
| &#39;Concurrent LeastConnections&#39; | 16          | 10.278 ms |    NA |  1.10 |               4.0000 |                - |   2.94 KB |        1.20 |
| &#39;Concurrent WeightedRandom&#39;   | 16          |  9.579 ms |    NA |  1.02 |               3.0000 |                - |    2.8 KB |        1.15 |
