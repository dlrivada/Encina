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
| **&#39;Concurrent LeastConnections (lease)&#39;** | **1**           |  **9.567 ms** |    **NA** |  **1.28** |                    **-** |                **-** |  **32.72 KB** |       **22.28** |
| &#39;Concurrent LeastConnections&#39;         | 1           |  8.961 ms |    NA |  1.20 |                    - |                - |  32.72 KB |       22.28 |
| &#39;Concurrent Random&#39;                   | 1           |  7.549 ms |    NA |  1.01 |                    - |                - |   1.47 KB |        1.00 |
| &#39;Concurrent RoundRobin&#39;               | 1           |  7.456 ms |    NA |  1.00 |                    - |                - |   1.47 KB |        1.00 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent LeastConnections (lease)&#39;** | **4**           | **11.317 ms** |    **NA** |  **1.23** |               **3.0000** |           **2.0000** |  **33.38 KB** |       **19.24** |
| &#39;Concurrent LeastConnections&#39;         | 4           | 10.818 ms |    NA |  1.18 |               3.0000 |           9.0000 |  33.38 KB |       19.24 |
| &#39;Concurrent Random&#39;                   | 4           |  9.775 ms |    NA |  1.06 |               1.0000 |                - |   1.73 KB |        1.00 |
| &#39;Concurrent RoundRobin&#39;               | 4           |  9.181 ms |    NA |  1.00 |               1.0000 |                - |   1.73 KB |        1.00 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent LeastConnections (lease)&#39;** | **8**           | **11.627 ms** |    **NA** |  **1.25** |               **5.0000** |           **4.0000** |  **33.83 KB** |       **17.32** |
| &#39;Concurrent LeastConnections&#39;         | 8           | 11.496 ms |    NA |  1.24 |               5.0000 |           9.0000 |  33.83 KB |       17.32 |
| &#39;Concurrent Random&#39;                   | 8           |  9.306 ms |    NA |  1.00 |               1.0000 |                - |   1.95 KB |        1.00 |
| &#39;Concurrent RoundRobin&#39;               | 8           |  9.265 ms |    NA |  1.00 |               1.0000 |                - |   1.95 KB |        1.00 |
|                                       |             |           |       |       |                      |                  |           |             |
| **&#39;Concurrent LeastConnections (lease)&#39;** | **16**          | **11.389 ms** |    **NA** |  **1.18** |               **5.0000** |           **3.0000** |  **34.32 KB** |       **14.04** |
| &#39;Concurrent LeastConnections&#39;         | 16          | 10.950 ms |    NA |  1.14 |               5.0000 |          11.0000 |  34.45 KB |       14.09 |
| &#39;Concurrent Random&#39;                   | 16          |  9.441 ms |    NA |  0.98 |               1.0000 |                - |   2.45 KB |        1.00 |
| &#39;Concurrent RoundRobin&#39;               | 16          |  9.612 ms |    NA |  1.00 |               1.0000 |                - |   2.45 KB |        1.00 |
