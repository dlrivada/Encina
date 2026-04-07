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
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **12.519 ms** |    **NA** |  **1.00** |    **3** |    **6496 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |  1.499 ms |    NA |  0.12 |    1 |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 13.528 ms |    NA |  1.08 |    5 |    6808 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 13.389 ms |    NA |  1.07 |    4 |    6496 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 12.423 ms |    NA |  0.99 |    2 |    6496 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **12.439 ms** |    **NA** |  **1.00** |    **2** |   **22696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |  1.617 ms |    NA |  0.13 |    1 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 13.963 ms |    NA |  1.12 |    5 |   23008 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 13.878 ms |    NA |  1.12 |    4 |   23648 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 12.849 ms |    NA |  1.03 |    3 |   22696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **12.913 ms** |    **NA** |  **1.00** |    **2** |   **19696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |  1.573 ms |    NA |  0.12 |    1 |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 13.958 ms |    NA |  1.08 |    4 |   20008 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 14.009 ms |    NA |  1.08 |    5 |   20648 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 13.207 ms |    NA |  1.02 |    3 |   19696 B |        1.00 |
|                                                       |            |               |           |       |       |      |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **13.516 ms** |    **NA** |  **1.00** |    **2** |  **154696 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |  1.707 ms |    NA |  0.13 |    1 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 14.255 ms |    NA |  1.05 |    4 |  155008 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 14.468 ms |    NA |  1.07 |    5 |  155648 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 13.830 ms |    NA |  1.02 |    3 |  154696 B |        1.00 |
