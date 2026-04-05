
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                                                | ShardCount | ItemsPerShard | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |----------:|------:|------:|-----:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **12.566 ms** |    **NA** |  **1.00** |    **3** |    **6496 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |  1.522 ms |    NA |  0.12 |    1 |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 13.245 ms |    NA |  1.05 |    5 |    6808 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 13.124 ms |    NA |  1.04 |    4 |    6496 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 12.351 ms |    NA |  0.98 |    2 |    6496 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **12.212 ms** |    **NA** |  **1.00** |    **2** |   **22696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |  1.535 ms |    NA |  0.13 |    1 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 13.765 ms |    NA |  1.13 |    5 |   23008 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 13.356 ms |    NA |  1.09 |    4 |   23648 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 12.315 ms |    NA |  1.01 |    3 |   22696 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **10**         | **10**            | **12.366 ms** |    **NA** |  **1.00** |    **2** |   **10696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 10         | 10            |  1.529 ms |    NA |  0.12 |    1 |     936 B |        0.09 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 10         | 10            | 13.155 ms |    NA |  1.06 |    4 |   11008 B |        1.03 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 10         | 10            | 13.218 ms |    NA |  1.07 |    5 |   10696 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 10         | 10            | 12.377 ms |    NA |  1.00 |    3 |   10696 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **10**         | **100**           | **12.379 ms** |    **NA** |  **1.00** |    **2** |   **64696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 10         | 100           |  1.486 ms |    NA |  0.12 |    1 |    8136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 10         | 100           | 13.379 ms |    NA |  1.08 |    4 |   65008 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 10         | 100           | 13.580 ms |    NA |  1.10 |    5 |   65648 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 10         | 100           | 12.653 ms |    NA |  1.02 |    3 |   64696 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **12.400 ms** |    **NA** |  **1.00** |    **2** |   **19696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |  1.490 ms |    NA |  0.12 |    1 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 13.497 ms |    NA |  1.09 |    5 |   20008 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 13.377 ms |    NA |  1.08 |    4 |   20648 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 12.460 ms |    NA |  1.00 |    3 |   19696 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **12.866 ms** |    **NA** |  **1.00** |    **2** |  **154696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |  1.550 ms |    NA |  0.12 |    1 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 13.950 ms |    NA |  1.08 |    5 |  155008 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 13.903 ms |    NA |  1.08 |    4 |  155648 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 13.254 ms |    NA |  1.03 |    3 |  154696 B |        1.00 |
