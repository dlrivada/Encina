
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.109 μs** |  **2.7947 μs** | **0.1532 μs** |  **1.00** |    **0.06** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.987 μs |  1.1870 μs | 0.0651 μs |  0.64 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.535 μs |  0.6578 μs | 0.0361 μs |  0.82 |    0.04 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.725 μs |  4.6305 μs | 0.2538 μs |  1.20 |    0.09 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.831 μs | 18.8511 μs | 1.0333 μs |  2.20 |    0.30 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.470 μs |  6.5681 μs | 0.3600 μs |  1.12 |    0.11 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.349 μs |  1.9677 μs | 0.1079 μs |  1.08 |    0.05 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.633 μs |  5.2068 μs | 0.2854 μs |  1.81 |    0.11 |    4 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.229 μs** |  **3.5508 μs** | **0.1946 μs** |  **1.00** |    **0.06** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.107 μs |  1.0048 μs | 0.0551 μs |  0.50 |    0.02 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.558 μs |  1.1777 μs | 0.0646 μs |  0.61 |    0.03 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.085 μs |  1.8365 μs | 0.1007 μs |  0.97 |    0.04 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.645 μs |  6.4020 μs | 0.3509 μs |  1.81 |    0.10 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.256 μs |  1.4504 μs | 0.0795 μs |  0.77 |    0.03 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.687 μs |  2.5607 μs | 0.1404 μs |  0.87 |    0.04 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.643 μs |  8.5443 μs | 0.4683 μs |  1.34 |    0.11 |    4 |     152 B |        2.71 |
