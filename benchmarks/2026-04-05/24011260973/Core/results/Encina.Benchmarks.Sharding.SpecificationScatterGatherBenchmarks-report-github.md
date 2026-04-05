```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                                                | ShardCount | ItemsPerShard | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |----------:|------:|------:|-----:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **12.502 ms** |    **NA** |  **1.00** |    **3** |    **6496 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |  1.762 ms |    NA |  0.14 |    1 |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 14.080 ms |    NA |  1.13 |    5 |   19144 B |        2.95 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 13.196 ms |    NA |  1.06 |    4 |    6496 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 12.354 ms |    NA |  0.99 |    2 |    6496 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **12.315 ms** |    **NA** |  **1.00** |    **2** |   **22696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |  1.523 ms |    NA |  0.12 |    1 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 13.368 ms |    NA |  1.09 |    5 |   23008 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 13.237 ms |    NA |  1.07 |    4 |   23648 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 12.452 ms |    NA |  1.01 |    3 |   22696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **10**         | **10**            | **12.191 ms** |    **NA** |  **1.00** |    **2** |   **23032 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 10         | 10            |  1.492 ms |    NA |  0.12 |    1 |     936 B |        0.04 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 10         | 10            | 13.366 ms |    NA |  1.10 |    5 |   11008 B |        0.48 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 10         | 10            | 13.189 ms |    NA |  1.08 |    4 |   10696 B |        0.46 |
| &#39;MergeAndOrder with descending ordering&#39;              | 10         | 10            | 12.229 ms |    NA |  1.00 |    3 |   10696 B |        0.46 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **10**         | **100**           | **12.612 ms** |    **NA** |  **1.00** |    **3** |   **64696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 10         | 100           |  1.507 ms |    NA |  0.12 |    1 |    8136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 10         | 100           | 13.343 ms |    NA |  1.06 |    4 |   65008 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 10         | 100           | 13.460 ms |    NA |  1.07 |    5 |   65648 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 10         | 100           | 12.561 ms |    NA |  1.00 |    2 |   64696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **12.440 ms** |    **NA** |  **1.00** |    **2** |   **19696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |  1.488 ms |    NA |  0.12 |    1 |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 13.408 ms |    NA |  1.08 |    5 |   20008 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 13.321 ms |    NA |  1.07 |    4 |   20648 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 12.460 ms |    NA |  1.00 |    3 |   19696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **12.844 ms** |    **NA** |  **1.00** |    **2** |  **154696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |  1.483 ms |    NA |  0.12 |    1 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 13.642 ms |    NA |  1.06 |    4 |  155008 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 13.758 ms |    NA |  1.07 |    5 |  155648 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 13.208 ms |    NA |  1.03 |    3 |  154696 B |        1.00 |
