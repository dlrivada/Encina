
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.239 μs** |  **0.7669 μs** | **0.0420 μs** |  **1.00** |    **0.02** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.414 μs |  1.3181 μs | 0.0723 μs |  0.75 |    0.02 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.454 μs |  0.9480 μs | 0.0520 μs |  0.76 |    0.02 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.231 μs |  7.7145 μs | 0.4229 μs |  1.31 |    0.11 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.307 μs |  7.0785 μs | 0.3880 μs |  1.95 |    0.11 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.878 μs | 22.1577 μs | 1.2145 μs |  1.20 |    0.33 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 4.062 μs |  5.7513 μs | 0.3152 μs |  1.25 |    0.09 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.558 μs |  5.0040 μs | 0.2743 μs |  1.72 |    0.08 |    4 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.453 μs** |  **3.2448 μs** | **0.1779 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.534 μs | 10.8192 μs | 0.5930 μs |  0.74 |    0.15 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.478 μs |  1.4746 μs | 0.0808 μs |  0.72 |    0.04 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.369 μs | 11.8143 μs | 0.6476 μs |  1.27 |    0.17 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.607 μs |  4.1697 μs | 0.2286 μs |  2.21 |    0.11 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.514 μs |  4.3890 μs | 0.2406 μs |  1.02 |    0.08 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.244 μs |  7.1724 μs | 0.3931 μs |  1.23 |    0.11 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 7.117 μs |  2.6792 μs | 0.1469 μs |  2.06 |    0.10 |    3 |     152 B |        2.71 |
