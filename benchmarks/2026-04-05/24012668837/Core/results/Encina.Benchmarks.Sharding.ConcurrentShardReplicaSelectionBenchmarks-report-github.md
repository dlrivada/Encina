```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                        | ThreadCount | Mean      | Error | Ratio | Completed Work Items | Lock Contentions | Allocated | Alloc Ratio |
|------------------------------ |------------ |----------:|------:|------:|---------------------:|-----------------:|----------:|------------:|
| **&#39;Concurrent RoundRobin&#39;**       | **1**           |  **8.124 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 1           |  7.915 ms |    NA |  0.97 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 1           |  8.759 ms |    NA |  1.08 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 1           |  8.390 ms |    NA |  1.03 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 1           |  8.484 ms |    NA |  1.04 |                    - |                - |   1.47 KB |        1.00 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **4**           |  **9.448 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 4           |  9.668 ms |    NA |  1.02 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 4           | 10.317 ms |    NA |  1.09 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent LeastConnections&#39; | 4           | 10.730 ms |    NA |  1.14 |               3.0000 |                - |   2.15 KB |        1.24 |
| &#39;Concurrent WeightedRandom&#39;   | 4           | 10.202 ms |    NA |  1.08 |               3.0000 |                - |   2.07 KB |        1.19 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **8**           | **10.227 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 8           |  9.818 ms |    NA |  0.96 |               1.0000 |                - |   1.95 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 8           | 11.136 ms |    NA |  1.09 |               3.0000 |                - |   2.29 KB |        1.17 |
| &#39;Concurrent LeastConnections&#39; | 8           | 10.701 ms |    NA |  1.05 |               4.0000 |                - |   2.47 KB |        1.26 |
| &#39;Concurrent WeightedRandom&#39;   | 8           | 10.275 ms |    NA |  1.00 |               3.0000 |                - |   2.27 KB |        1.16 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **16**          | **10.020 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 16          | 10.232 ms |    NA |  1.02 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 16          | 10.914 ms |    NA |  1.09 |               4.0000 |                - |   2.91 KB |        1.19 |
| &#39;Concurrent LeastConnections&#39; | 16          | 11.400 ms |    NA |  1.14 |               4.0000 |                - |   2.91 KB |        1.19 |
| &#39;Concurrent WeightedRandom&#39;   | 16          | 10.571 ms |    NA |  1.06 |               2.0000 |                - |   2.63 KB |        1.07 |
