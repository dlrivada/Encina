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
| **&#39;Concurrent RoundRobin&#39;**       | **1**           |  **7.320 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 1           |  7.431 ms |    NA |  1.02 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 1           |  8.144 ms |    NA |  1.11 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 1           |  8.272 ms |    NA |  1.13 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 1           |  8.001 ms |    NA |  1.09 |                    - |                - |   1.47 KB |        1.00 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **4**           |  **9.054 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 4           |  8.996 ms |    NA |  0.99 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 4           |  9.799 ms |    NA |  1.08 |               3.0000 |                - |   2.05 KB |        1.18 |
| &#39;Concurrent LeastConnections&#39; | 4           |  9.967 ms |    NA |  1.10 |               3.0000 |                - |   2.15 KB |        1.24 |
| &#39;Concurrent WeightedRandom&#39;   | 4           |  9.340 ms |    NA |  1.03 |               3.0000 |                - |   2.05 KB |        1.18 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **8**           |  **9.197 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 8           |  9.614 ms |    NA |  1.05 |               2.0000 |                - |   2.11 KB |        1.08 |
| &#39;Concurrent LeastLatency&#39;     | 8           |  9.810 ms |    NA |  1.07 |               4.0000 |                - |   2.42 KB |        1.24 |
| &#39;Concurrent LeastConnections&#39; | 8           | 10.037 ms |    NA |  1.09 |               5.0000 |                - |   2.58 KB |        1.32 |
| &#39;Concurrent WeightedRandom&#39;   | 8           |  9.497 ms |    NA |  1.03 |               3.0000 |                - |   2.29 KB |        1.17 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **16**          |  **9.108 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 16          |  9.145 ms |    NA |  1.00 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 16          | 10.013 ms |    NA |  1.10 |               4.0000 |                - |   2.91 KB |        1.19 |
| &#39;Concurrent LeastConnections&#39; | 16          | 10.110 ms |    NA |  1.11 |               4.0000 |                - |   2.98 KB |        1.22 |
| &#39;Concurrent WeightedRandom&#39;   | 16          |  9.415 ms |    NA |  1.03 |               3.0000 |                - |   2.78 KB |        1.14 |
