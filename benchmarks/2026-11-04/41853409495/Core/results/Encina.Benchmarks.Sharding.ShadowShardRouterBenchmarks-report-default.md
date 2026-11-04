
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.230 μs** |  **0.7456 μs** | **0.0409 μs** |  **1.00** |    **0.02** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.956 μs |  0.8999 μs | 0.0493 μs |  1.22 |    0.02 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.444 μs |  5.0249 μs | 0.2754 μs |  1.38 |    0.08 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.952 μs | 21.2923 μs | 1.1671 μs |  1.22 |    0.31 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.556 μs |  3.7130 μs | 0.2035 μs |  1.10 |    0.06 |    1 |      64 B |        1.14 |
                                          |            |          |            |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.530 μs** |  **3.7669 μs** | **0.2065 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.719 μs |  4.3498 μs | 0.2384 μs |  1.06 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.176 μs |  9.1190 μs | 0.4998 μs |  1.47 |    0.14 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.457 μs |  3.3461 μs | 0.1834 μs |  0.98 |    0.07 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.013 μs |  5.2885 μs | 0.2899 μs |  1.14 |    0.09 |    1 |      64 B |        1.14 |
