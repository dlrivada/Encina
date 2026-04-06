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
| **&#39;Concurrent RoundRobin&#39;**       | **1**           |  **7.623 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 1           |  7.680 ms |    NA |  1.01 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 1           |  8.423 ms |    NA |  1.11 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 1           |  8.720 ms |    NA |  1.14 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 1           |  8.326 ms |    NA |  1.09 |                    - |                - |   1.47 KB |        1.00 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **4**           |  **9.355 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 4           |  9.676 ms |    NA |  1.03 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 4           | 10.339 ms |    NA |  1.11 |               3.0000 |                - |   2.15 KB |        1.24 |
| &#39;Concurrent LeastConnections&#39; | 4           | 10.518 ms |    NA |  1.12 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent WeightedRandom&#39;   | 4           |  9.897 ms |    NA |  1.06 |               2.0000 |                - |   1.91 KB |        1.10 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **8**           |  **9.769 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 8           |  9.471 ms |    NA |  0.97 |               2.0000 |                - |   2.11 KB |        1.08 |
| &#39;Concurrent LeastLatency&#39;     | 8           | 10.533 ms |    NA |  1.08 |               4.0000 |                - |   2.45 KB |        1.25 |
| &#39;Concurrent LeastConnections&#39; | 8           | 10.844 ms |    NA |  1.11 |               3.0000 |                - |   2.31 KB |        1.18 |
| &#39;Concurrent WeightedRandom&#39;   | 8           | 10.060 ms |    NA |  1.03 |               3.0000 |                - |   2.29 KB |        1.17 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **16**          |  **9.759 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 16          |  9.645 ms |    NA |  0.99 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 16          | 10.626 ms |    NA |  1.09 |               4.0000 |                - |   2.91 KB |        1.19 |
| &#39;Concurrent LeastConnections&#39; | 16          | 10.886 ms |    NA |  1.12 |               3.0000 |                - |   2.76 KB |        1.13 |
| &#39;Concurrent WeightedRandom&#39;   | 16          | 10.065 ms |    NA |  1.03 |               3.0000 |                - |   2.83 KB |        1.16 |
