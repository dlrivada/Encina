
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.36GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **88,630.6 ns** | **10,313.62 ns** |   **565.32 ns** | **1.000** |    **2** | **0.2441** | **0.1221** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     346.4 ns |      9.35 ns |     0.51 ns | 0.004 |    1 | 0.0148 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  92,304.3 ns | 12,795.54 ns |   701.37 ns | 1.041 |    2 | 0.2441 | 0.1221 |    6846 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  88,589.2 ns | 10,902.55 ns |   597.61 ns | 1.000 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  88,483.6 ns | 11,551.18 ns |   633.16 ns | 0.998 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **133,817.5 ns** | **23,496.34 ns** | **1,287.91 ns** | **1.000** |    **2** | **0.7324** | **0.4883** |   **22731 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     609.2 ns |    234.82 ns |    12.87 ns | 0.005 |    1 | 0.1001 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 135,633.8 ns |  3,446.98 ns |   188.94 ns | 1.014 |    2 | 0.7324 | 0.4883 |   23043 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 133,406.7 ns | 12,632.25 ns |   692.42 ns | 0.997 |    2 | 0.7324 | 0.4883 |   23683 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 148,299.5 ns |  9,329.66 ns |   511.39 ns | 1.108 |    2 | 0.7324 | 0.4883 |   22731 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **119,816.5 ns** |  **9,858.63 ns** |   **540.39 ns** |  **1.00** |    **2** | **0.7324** | **0.4883** |   **19734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,540.4 ns |     58.64 ns |     3.21 ns |  0.01 |    1 | 0.0839 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 122,755.8 ns |  8,253.03 ns |   452.38 ns |  1.02 |    2 | 0.7324 | 0.6104 |   20046 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 122,575.4 ns | 18,606.47 ns | 1,019.88 ns |  1.02 |    2 | 0.7324 | 0.4883 |   20685 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 134,687.1 ns | 15,583.59 ns |   854.19 ns |  1.12 |    2 | 0.7324 | 0.4883 |   19734 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **642,206.9 ns** | **15,442.14 ns** |   **846.44 ns** | **1.000** |    **2** | **5.8594** | **2.9297** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   3,406.5 ns |  1,533.46 ns |    84.05 ns | 0.005 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 665,146.2 ns | 16,886.90 ns |   925.63 ns | 1.036 |    2 | 5.8594 | 4.8828 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 656,744.8 ns | 13,477.38 ns |   738.74 ns | 1.023 |    2 | 5.8594 | 2.9297 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 943,294.9 ns | 26,523.59 ns | 1,453.85 ns | 1.469 |    3 | 5.8594 | 3.9063 |  154727 B |        1.00 |
