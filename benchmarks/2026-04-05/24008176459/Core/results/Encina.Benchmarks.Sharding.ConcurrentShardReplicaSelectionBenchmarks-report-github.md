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
| &#39;Concurrent Random&#39;           | 1           |  8.017 ms |    NA |  1.05 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 1           |  8.652 ms |    NA |  1.13 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 1           |  8.613 ms |    NA |  1.13 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 1           |  8.191 ms |    NA |  1.07 |                    - |                - |   1.47 KB |        1.00 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **4**           |  **9.575 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 4           |  9.330 ms |    NA |  0.97 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 4           | 11.030 ms |    NA |  1.15 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent LeastConnections&#39; | 4           | 11.200 ms |    NA |  1.17 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent WeightedRandom&#39;   | 4           | 10.305 ms |    NA |  1.08 |               3.0000 |                - |   2.07 KB |        1.19 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **8**           |  **9.613 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 8           |  9.612 ms |    NA |  1.00 |               1.0000 |                - |   1.95 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 8           | 10.529 ms |    NA |  1.10 |               3.0000 |                - |   2.27 KB |        1.16 |
| &#39;Concurrent LeastConnections&#39; | 8           | 10.609 ms |    NA |  1.10 |               3.0000 |                - |   2.29 KB |        1.17 |
| &#39;Concurrent WeightedRandom&#39;   | 8           | 10.340 ms |    NA |  1.08 |               2.0000 |                - |   2.11 KB |        1.08 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **16**          |  **9.657 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 16          |  9.691 ms |    NA |  1.00 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 16          | 10.521 ms |    NA |  1.09 |               3.0000 |                - |    2.8 KB |        1.15 |
| &#39;Concurrent LeastConnections&#39; | 16          | 10.625 ms |    NA |  1.10 |               4.0000 |                - |   2.94 KB |        1.20 |
| &#39;Concurrent WeightedRandom&#39;   | 16          | 10.468 ms |    NA |  1.08 |               3.0000 |                - |   2.76 KB |        1.13 |
