
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.600 μs** |  **7.377 μs** | **0.4043 μs** | **3.788 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.092 μs |  1.100 μs | 0.0603 μs | 3.086 μs |  0.87 |    0.09 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.110 μs |  3.486 μs | 0.1911 μs | 4.116 μs |  1.15 |    0.13 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.199 μs |  2.930 μs | 0.1606 μs | 3.206 μs |  0.90 |    0.10 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.379 μs |  1.100 μs | 0.0603 μs | 3.386 μs |  0.95 |    0.10 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.119 μs** |  **2.036 μs** | **0.1116 μs** | **4.133 μs** |  **1.00** |    **0.03** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.432 μs |  2.434 μs | 0.1334 μs | 3.365 μs |  0.83 |    0.03 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.121 μs | 40.849 μs | 2.2391 μs | 4.848 μs |  1.49 |    0.47 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.340 μs |  1.699 μs | 0.0932 μs | 3.296 μs |  0.81 |    0.03 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.121 μs |  6.149 μs | 0.3370 μs | 3.957 μs |  1.00 |    0.07 |    1 |      64 B |        1.14 |
