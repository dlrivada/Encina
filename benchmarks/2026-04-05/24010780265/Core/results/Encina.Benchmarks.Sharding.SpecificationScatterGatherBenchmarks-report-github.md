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
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **12.246 ms** |    **NA** |  **1.00** |    **2** |    **6496 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |  1.515 ms |    NA |  0.12 |    1 |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 13.167 ms |    NA |  1.08 |    3 |    6808 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 13.198 ms |    NA |  1.08 |    4 |    6496 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 16.348 ms |    NA |  1.33 |    5 |    6496 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **12.513 ms** |    **NA** |  **1.00** |    **3** |   **22696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |  2.280 ms |    NA |  0.18 |    1 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 13.238 ms |    NA |  1.06 |    4 |   23008 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 13.255 ms |    NA |  1.06 |    5 |   23648 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 12.478 ms |    NA |  1.00 |    2 |   22696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **10**         | **10**            | **12.507 ms** |    **NA** |  **1.00** |    **3** |   **10696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 10         | 10            |  1.495 ms |    NA |  0.12 |    1 |     936 B |        0.09 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 10         | 10            | 13.358 ms |    NA |  1.07 |    5 |   11008 B |        1.03 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 10         | 10            | 13.133 ms |    NA |  1.05 |    4 |   10696 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 10         | 10            | 12.397 ms |    NA |  0.99 |    2 |   23032 B |        2.15 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **10**         | **100**           | **12.441 ms** |    **NA** |  **1.00** |    **2** |   **64696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 10         | 100           |  1.487 ms |    NA |  0.12 |    1 |    8136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 10         | 100           | 13.498 ms |    NA |  1.09 |    4 |   65008 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 10         | 100           | 13.549 ms |    NA |  1.09 |    5 |   65648 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 10         | 100           | 13.001 ms |    NA |  1.05 |    3 |   64696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **12.239 ms** |    **NA** |  **1.00** |    **2** |   **19696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |  1.521 ms |    NA |  0.12 |    1 |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 13.184 ms |    NA |  1.08 |    4 |   20008 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 13.212 ms |    NA |  1.08 |    5 |   20648 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 12.376 ms |    NA |  1.01 |    3 |   19696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **12.732 ms** |    **NA** |  **1.00** |    **2** |  **154696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |  1.543 ms |    NA |  0.12 |    1 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 13.976 ms |    NA |  1.10 |    5 |  167344 B |        1.08 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 13.862 ms |    NA |  1.09 |    4 |  155648 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 13.028 ms |    NA |  1.02 |    3 |  154696 B |        1.00 |
