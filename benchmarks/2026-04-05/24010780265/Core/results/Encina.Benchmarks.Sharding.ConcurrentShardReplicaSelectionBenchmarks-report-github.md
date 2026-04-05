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
| **&#39;Concurrent RoundRobin&#39;**       | **1**           |  **7.642 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 1           |  7.608 ms |    NA |  1.00 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 1           |  8.188 ms |    NA |  1.07 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 1           |  8.318 ms |    NA |  1.09 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 1           |  7.976 ms |    NA |  1.04 |                    - |                - |   1.47 KB |        1.00 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **4**           |  **8.863 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 4           |  9.040 ms |    NA |  1.02 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 4           |  9.596 ms |    NA |  1.08 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent LeastConnections&#39; | 4           |  9.861 ms |    NA |  1.11 |               3.0000 |                - |   2.05 KB |        1.18 |
| &#39;Concurrent WeightedRandom&#39;   | 4           |  9.239 ms |    NA |  1.04 |               3.0000 |                - |   2.05 KB |        1.18 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **8**           |  **8.986 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 8           |  9.062 ms |    NA |  1.01 |               1.0000 |                - |   1.95 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 8           |  9.877 ms |    NA |  1.10 |               4.0000 |                - |   2.45 KB |        1.25 |
| &#39;Concurrent LeastConnections&#39; | 8           | 10.195 ms |    NA |  1.13 |               4.0000 |                - |   2.42 KB |        1.24 |
| &#39;Concurrent WeightedRandom&#39;   | 8           |  9.617 ms |    NA |  1.07 |               3.0000 |                - |   2.29 KB |        1.17 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **16**          |  **9.369 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 16          |  9.073 ms |    NA |  0.97 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 16          |  9.932 ms |    NA |  1.06 |               4.0000 |                - |   2.94 KB |        1.20 |
| &#39;Concurrent LeastConnections&#39; | 16          | 10.060 ms |    NA |  1.07 |               4.0000 |                - |   2.94 KB |        1.20 |
| &#39;Concurrent WeightedRandom&#39;   | 16          |  9.625 ms |    NA |  1.03 |               3.0000 |                - |   2.76 KB |        1.13 |
