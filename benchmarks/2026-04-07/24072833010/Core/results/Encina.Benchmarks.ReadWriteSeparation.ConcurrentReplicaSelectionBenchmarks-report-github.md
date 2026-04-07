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
| **&#39;Concurrent LeastConnections (lease)&#39;** | **1**           |  **9.389 ms** |    **NA** |  **1.25** |                    **-** |                **-** |  **32.72 KB** |       **22.28** |
| &#39;Concurrent LeastConnections&#39;         | 1           |  8.903 ms |    NA |  1.18 |                    - |                - |  32.72 KB |       22.28 |
| &#39;Concurrent Random&#39;                   | 1           |  7.541 ms |    NA |  1.00 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent RoundRobin&#39;               | 1           |  7.538 ms |    NA |  1.00 |                    - |                - |   1.47 KB |        1.00 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent LeastConnections (lease)&#39;** | **4**           | **11.455 ms** |    **NA** |  **1.25** |               **3.0000** |           **2.0000** |   **33.3 KB** |       **19.20** |
| &#39;Concurrent LeastConnections&#39;         | 4           | 10.995 ms |    NA |  1.20 |               3.0000 |          10.0000 |   33.3 KB |       19.20 |
| &#39;Concurrent Random&#39;                   | 4           |  9.087 ms |    NA |  0.99 |               2.0000 |                - |   1.89 KB |        1.09 |
| &#39;Concurrent RoundRobin&#39;               | 4           |  9.145 ms |    NA |  1.00 |               1.0000 |                - |   1.73 KB |        1.00 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent LeastConnections (lease)&#39;** | **8**           | **11.344 ms** |    **NA** |  **1.21** |               **5.0000** |           **3.0000** |  **33.95 KB** |       **17.38** |
| &#39;Concurrent LeastConnections&#39;         | 8           | 10.798 ms |    NA |  1.15 |               5.0000 |          13.0000 |  33.87 KB |       17.34 |
| &#39;Concurrent Random&#39;                   | 8           |  9.122 ms |    NA |  0.97 |               2.0000 |                - |   2.11 KB |        1.08 |
| &#39;Concurrent RoundRobin&#39;               | 8           |  9.390 ms |    NA |  1.00 |               1.0000 |                - |   1.95 KB |        1.00 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent LeastConnections (lease)&#39;** | **16**          | **11.545 ms** |    **NA** |  **1.23** |               **5.0000** |           **3.0000** |  **34.38 KB** |       **14.06** |
| &#39;Concurrent LeastConnections&#39;         | 16          | 10.791 ms |    NA |  1.15 |               5.0000 |           9.0000 |  34.36 KB |       14.05 |
| &#39;Concurrent Random&#39;                   | 16          |  9.649 ms |    NA |  1.03 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent RoundRobin&#39;               | 16          |  9.350 ms |    NA |  1.00 |               1.0000 |                - |   2.45 KB |        1.00 |
