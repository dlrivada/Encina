
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.240 μs** | **0.1555 μs** | **0.2230 μs** |  **1.00** |    **0.09** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.187 μs | 0.1782 μs | 0.2612 μs |  0.68 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.587 μs | 0.0869 μs | 0.1274 μs |  0.80 |    0.06 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.831 μs | 0.1606 μs | 0.2354 μs |  1.19 |    0.10 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 7.008 μs | 0.4771 μs | 0.6843 μs |  2.17 |    0.25 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.173 μs | 0.0903 μs | 0.1236 μs |  0.98 |    0.07 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.542 μs | 0.1763 μs | 0.2529 μs |  1.10 |    0.10 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.551 μs | 0.1552 μs | 0.2225 μs |  1.72 |    0.13 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.798 μs** | **0.2733 μs** | **0.3831 μs** |  **1.01** |    **0.14** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.715 μs | 0.2305 μs | 0.3379 μs |  0.72 |    0.11 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.873 μs | 0.2491 μs | 0.3492 μs |  0.76 |    0.12 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.231 μs | 0.5350 μs | 0.7500 μs |  1.13 |    0.23 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.279 μs | 0.5337 μs | 0.7655 μs |  2.20 |    0.30 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.257 μs | 0.0977 μs | 0.1401 μs |  0.87 |    0.09 |    1 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.643 μs | 0.1618 μs | 0.2268 μs |  1.23 |    0.14 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.526 μs | 0.1583 μs | 0.2320 μs |  1.74 |    0.18 |    3 |     152 B |        2.71 |
