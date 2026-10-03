
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.567 μs** |  **3.245 μs** | **0.1779 μs** | **2.604 μs** |  **1.00** |    **0.09** |    **3** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 1.485 μs | 21.890 μs | 1.1999 μs | 1.040 μs |  0.58 |    0.41 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 2.092 μs | 20.422 μs | 1.1194 μs | 1.538 μs |  0.82 |    0.38 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 1.601 μs | 21.460 μs | 1.1763 μs | 1.297 μs |  0.63 |    0.40 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.631 μs |  5.689 μs | 0.3119 μs | 2.604 μs |  1.03 |    0.12 |    3 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **1.957 μs** | **15.188 μs** | **0.8325 μs** | **1.543 μs** |  **1.11** |    **0.54** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.033 μs | 17.091 μs | 0.9368 μs | 1.503 μs |  1.15 |    0.59 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.430 μs | 19.922 μs | 1.0920 μs | 3.866 μs |  2.51 |    0.93 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 1.853 μs | 20.063 μs | 1.0997 μs | 1.413 μs |  1.05 |    0.64 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 2.110 μs | 23.977 μs | 1.3143 μs | 1.412 μs |  1.20 |    0.76 |    1 |      64 B |        1.14 |
