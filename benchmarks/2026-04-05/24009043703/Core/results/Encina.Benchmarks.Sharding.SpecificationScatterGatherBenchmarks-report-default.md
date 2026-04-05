
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                                                | ShardCount | ItemsPerShard | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |----------:|------:|------:|-----:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **12.209 ms** |    **NA** |  **1.00** |    **2** |    **6496 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |  1.552 ms |    NA |  0.13 |    1 |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 13.321 ms |    NA |  1.09 |    4 |    6808 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 13.326 ms |    NA |  1.09 |    5 |    6496 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 12.275 ms |    NA |  1.01 |    3 |    6496 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **12.314 ms** |    **NA** |  **1.00** |    **2** |   **22696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |  1.519 ms |    NA |  0.12 |    1 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 13.295 ms |    NA |  1.08 |    4 |   23008 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 13.469 ms |    NA |  1.09 |    5 |   23648 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 12.452 ms |    NA |  1.01 |    3 |   22696 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **10**         | **10**            | **12.127 ms** |    **NA** |  **1.00** |    **2** |   **10696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 10         | 10            |  1.497 ms |    NA |  0.12 |    1 |     936 B |        0.09 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 10         | 10            | 14.176 ms |    NA |  1.17 |    5 |   11008 B |        1.03 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 10         | 10            | 13.311 ms |    NA |  1.10 |    4 |   23032 B |        2.15 |
 'MergeAndOrder with descending ordering'              | 10         | 10            | 12.451 ms |    NA |  1.03 |    3 |   10696 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **10**         | **100**           | **12.379 ms** |    **NA** |  **1.00** |    **2** |   **64696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 10         | 100           |  1.483 ms |    NA |  0.12 |    1 |    8136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 10         | 100           | 13.472 ms |    NA |  1.09 |    5 |   65008 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 10         | 100           | 13.430 ms |    NA |  1.08 |    4 |   65648 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 10         | 100           | 12.693 ms |    NA |  1.03 |    3 |   64696 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **12.326 ms** |    **NA** |  **1.00** |    **2** |   **19696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |  1.550 ms |    NA |  0.13 |    1 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 13.424 ms |    NA |  1.09 |    5 |   20008 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 13.372 ms |    NA |  1.08 |    4 |   20648 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 12.545 ms |    NA |  1.02 |    3 |   19696 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **13.034 ms** |    **NA** |  **1.00** |    **2** |  **154696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |  1.530 ms |    NA |  0.12 |    1 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 13.981 ms |    NA |  1.07 |    4 |  155008 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 13.993 ms |    NA |  1.07 |    5 |  155648 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 13.189 ms |    NA |  1.01 |    3 |  154696 B |        1.00 |
