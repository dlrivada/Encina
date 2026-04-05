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
| **&#39;Concurrent RoundRobin&#39;**       | **1**           |  **7.573 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 1           |  7.456 ms |    NA |  0.98 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 1           |  8.195 ms |    NA |  1.08 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 1           |  8.356 ms |    NA |  1.10 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 1           |  8.218 ms |    NA |  1.09 |                    - |                - |   1.47 KB |        1.00 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **4**           |  **8.983 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 4           |  8.871 ms |    NA |  0.99 |               2.0000 |                - |   1.89 KB |        1.09 |
| &#39;Concurrent LeastLatency&#39;     | 4           |  9.627 ms |    NA |  1.07 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent LeastConnections&#39; | 4           |  9.937 ms |    NA |  1.11 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent WeightedRandom&#39;   | 4           |  9.664 ms |    NA |  1.08 |               3.0000 |                - |   2.05 KB |        1.18 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **8**           |  **9.307 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 8           |  8.948 ms |    NA |  0.96 |               2.0000 |                - |   2.11 KB |        1.08 |
| &#39;Concurrent LeastLatency&#39;     | 8           |  9.792 ms |    NA |  1.05 |               4.0000 |                - |   2.42 KB |        1.24 |
| &#39;Concurrent LeastConnections&#39; | 8           | 10.277 ms |    NA |  1.10 |               4.0000 |                - |   2.45 KB |        1.25 |
| &#39;Concurrent WeightedRandom&#39;   | 8           |  9.640 ms |    NA |  1.04 |               3.0000 |                - |   2.27 KB |        1.16 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **16**          |  **9.096 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 16          |  9.145 ms |    NA |  1.01 |               2.0000 |                - |    2.6 KB |        1.06 |
| &#39;Concurrent LeastLatency&#39;     | 16          |  9.965 ms |    NA |  1.10 |               4.0000 |                - |   2.91 KB |        1.19 |
| &#39;Concurrent LeastConnections&#39; | 16          | 10.267 ms |    NA |  1.13 |               3.0000 |                - |   2.76 KB |        1.13 |
| &#39;Concurrent WeightedRandom&#39;   | 16          |  9.505 ms |    NA |  1.04 |               3.0000 |                - |   2.78 KB |        1.14 |
