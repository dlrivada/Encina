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
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **13.010 ms** |    **NA** |  **1.00** |    **3** |    **6496 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |  1.597 ms |    NA |  0.12 |    1 |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 20.816 ms |    NA |  1.60 |    5 |    6808 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 14.040 ms |    NA |  1.08 |    4 |    6496 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 12.818 ms |    NA |  0.99 |    2 |    6496 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **12.685 ms** |    **NA** |  **1.00** |    **2** |   **22696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |  1.535 ms |    NA |  0.12 |    1 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 14.271 ms |    NA |  1.13 |    4 |   23008 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 14.385 ms |    NA |  1.13 |    5 |   23648 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 13.034 ms |    NA |  1.03 |    3 |   22696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **10**         | **10**            | **12.742 ms** |    **NA** |  **1.00** |    **2** |   **10696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 10         | 10            |  1.542 ms |    NA |  0.12 |    1 |     936 B |        0.09 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 10         | 10            | 13.696 ms |    NA |  1.07 |    5 |   11008 B |        1.03 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 10         | 10            | 13.567 ms |    NA |  1.06 |    4 |   23032 B |        2.15 |
| &#39;MergeAndOrder with descending ordering&#39;              | 10         | 10            | 13.200 ms |    NA |  1.04 |    3 |   10696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **10**         | **100**           | **13.186 ms** |    **NA** |  **1.00** |    **3** |   **64696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 10         | 100           |  1.518 ms |    NA |  0.12 |    1 |    8136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 10         | 100           | 13.663 ms |    NA |  1.04 |    4 |   65008 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 10         | 100           | 13.942 ms |    NA |  1.06 |    5 |   77984 B |        1.21 |
| &#39;MergeAndOrder with descending ordering&#39;              | 10         | 100           | 12.894 ms |    NA |  0.98 |    2 |   64696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **12.843 ms** |    **NA** |  **1.00** |    **2** |   **19696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |  1.620 ms |    NA |  0.13 |    1 |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 13.593 ms |    NA |  1.06 |    5 |   20008 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 13.575 ms |    NA |  1.06 |    4 |   20648 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 12.895 ms |    NA |  1.00 |    3 |   19696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **13.168 ms** |    **NA** |  **1.00** |    **2** |  **154696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |  1.597 ms |    NA |  0.12 |    1 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 14.310 ms |    NA |  1.09 |    4 |  155008 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 14.472 ms |    NA |  1.10 |    5 |  155648 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 13.441 ms |    NA |  1.02 |    3 |  154696 B |        1.00 |
