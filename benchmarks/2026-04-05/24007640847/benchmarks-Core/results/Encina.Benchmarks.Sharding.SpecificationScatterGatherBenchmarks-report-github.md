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
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **12.627 ms** |    **NA** |  **1.00** |    **3** |    **6496 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |  1.577 ms |    NA |  0.12 |    1 |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 13.899 ms |    NA |  1.10 |    5 |    6808 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 13.439 ms |    NA |  1.06 |    4 |    6496 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 12.562 ms |    NA |  0.99 |    2 |    6496 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **12.681 ms** |    **NA** |  **1.00** |    **3** |   **22696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |  1.521 ms |    NA |  0.12 |    1 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 13.500 ms |    NA |  1.06 |    4 |   23008 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 13.570 ms |    NA |  1.07 |    5 |   23648 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 12.566 ms |    NA |  0.99 |    2 |   22696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **10**         | **10**            | **12.501 ms** |    **NA** |  **1.00** |    **2** |   **10696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 10         | 10            |  1.516 ms |    NA |  0.12 |    1 |     936 B |        0.09 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 10         | 10            | 13.520 ms |    NA |  1.08 |    5 |   23344 B |        2.18 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 10         | 10            | 13.430 ms |    NA |  1.07 |    4 |   10696 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 10         | 10            | 12.523 ms |    NA |  1.00 |    3 |   10696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **10**         | **100**           | **12.887 ms** |    **NA** |  **1.00** |    **2** |   **64696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 10         | 100           |  1.526 ms |    NA |  0.12 |    1 |    8136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 10         | 100           | 13.988 ms |    NA |  1.09 |    5 |   65008 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 10         | 100           | 13.931 ms |    NA |  1.08 |    4 |   65648 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 10         | 100           | 13.251 ms |    NA |  1.03 |    3 |   64696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **12.754 ms** |    **NA** |  **1.00** |    **3** |   **19696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |  1.575 ms |    NA |  0.12 |    1 |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 13.717 ms |    NA |  1.08 |    5 |   20008 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 13.480 ms |    NA |  1.06 |    4 |   20648 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 12.567 ms |    NA |  0.99 |    2 |   19696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **12.998 ms** |    **NA** |  **1.00** |    **2** |  **154696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |  1.642 ms |    NA |  0.13 |    1 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 13.968 ms |    NA |  1.07 |    4 |  155008 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 14.132 ms |    NA |  1.09 |    5 |  155648 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 13.362 ms |    NA |  1.03 |    3 |  154696 B |        1.00 |
