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
| **&#39;Concurrent RoundRobin&#39;**       | **1**           |  **7.486 ms** |    **NA** |  **1.00** |                    **-** |                **-** |   **1.47 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 1           |  7.636 ms |    NA |  1.02 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 1           |  8.367 ms |    NA |  1.12 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent LeastConnections&#39; | 1           |  8.611 ms |    NA |  1.15 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent WeightedRandom&#39;   | 1           |  8.071 ms |    NA |  1.08 |                    - |                - |   1.47 KB |        1.00 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **4**           |  **8.902 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.73 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 4           |  9.099 ms |    NA |  1.02 |               2.0000 |                - |   1.89 KB |        1.09 |
| &#39;Concurrent LeastLatency&#39;     | 4           |  9.876 ms |    NA |  1.11 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent LeastConnections&#39; | 4           |  9.890 ms |    NA |  1.11 |               3.0000 |                - |   2.13 KB |        1.23 |
| &#39;Concurrent WeightedRandom&#39;   | 4           |  9.547 ms |    NA |  1.07 |               3.0000 |                - |   2.05 KB |        1.18 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **8**           |  **9.248 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **1.95 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 8           |  9.293 ms |    NA |  1.00 |               1.0000 |                - |   1.95 KB |        1.00 |
| &#39;Concurrent LeastLatency&#39;     | 8           | 10.025 ms |    NA |  1.08 |               4.0000 |                - |   2.42 KB |        1.24 |
| &#39;Concurrent LeastConnections&#39; | 8           | 10.367 ms |    NA |  1.12 |               4.0000 |                - |   2.45 KB |        1.25 |
| &#39;Concurrent WeightedRandom&#39;   | 8           |  9.634 ms |    NA |  1.04 |               2.0000 |                - |   2.11 KB |        1.08 |
|                               |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent RoundRobin&#39;**       | **16**          |  **9.091 ms** |    **NA** |  **1.00** |               **1.0000** |                **-** |   **2.45 KB** |        **1.00** |
| &#39;Concurrent Random&#39;           | 16          |  9.157 ms |    NA |  1.01 |               2.0000 |                - |    2.6 KB |        1.06 |
| &#39;Concurrent LeastLatency&#39;     | 16          | 10.270 ms |    NA |  1.13 |               4.0000 |                - |   2.94 KB |        1.20 |
| &#39;Concurrent LeastConnections&#39; | 16          | 10.313 ms |    NA |  1.13 |               4.0000 |                - |   2.94 KB |        1.20 |
| &#39;Concurrent WeightedRandom&#39;   | 16          |  9.806 ms |    NA |  1.08 |               3.0000 |                - |   2.78 KB |        1.14 |
