
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                                                | ShardCount | ItemsPerShard | Mean      | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |----------:|------:|------:|-----:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **12.203 ms** |    **NA** |  **1.00** |    **3** |    **6496 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |  1.438 ms |    NA |  0.12 |    1 |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 12.951 ms |    NA |  1.06 |    4 |    6808 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 13.414 ms |    NA |  1.10 |    5 |    6496 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 12.202 ms |    NA |  1.00 |    2 |    6496 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **12.385 ms** |    **NA** |  **1.00** |    **3** |   **22696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |  1.446 ms |    NA |  0.12 |    1 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 13.247 ms |    NA |  1.07 |    5 |   23008 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 12.874 ms |    NA |  1.04 |    4 |   23648 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 12.250 ms |    NA |  0.99 |    2 |   22696 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **12.161 ms** |    **NA** |  **1.00** |    **2** |   **19696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |  1.518 ms |    NA |  0.12 |    1 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 13.242 ms |    NA |  1.09 |    5 |   20008 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 12.996 ms |    NA |  1.07 |    4 |   20648 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 12.431 ms |    NA |  1.02 |    3 |   19696 B |        1.00 |
                                                       |            |               |           |       |       |      |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **12.771 ms** |    **NA** |  **1.00** |    **3** |  **154696 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |  1.475 ms |    NA |  0.12 |    1 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 14.195 ms |    NA |  1.11 |    4 |  155008 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 14.445 ms |    NA |  1.13 |    5 |  155648 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 12.767 ms |    NA |  1.00 |    2 |  154696 B |        1.00 |
