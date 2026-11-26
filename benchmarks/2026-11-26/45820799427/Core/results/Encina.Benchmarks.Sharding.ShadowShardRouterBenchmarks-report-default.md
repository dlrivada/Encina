
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **4.300 μs** |  **8.353 μs** | **0.4579 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 4.892 μs | 29.344 μs | 1.6084 μs |  1.15 |    0.34 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 5.814 μs | 42.044 μs | 2.3046 μs |  1.36 |    0.48 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.854 μs | 15.629 μs | 0.8567 μs |  0.90 |    0.19 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.952 μs | 35.974 μs | 1.9719 μs |  1.16 |    0.41 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **5.591 μs** |  **7.653 μs** | **0.4195 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 5.399 μs | 10.813 μs | 0.5927 μs |  0.97 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.676 μs | 15.248 μs | 0.8358 μs |  1.20 |    0.15 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 5.063 μs | 10.301 μs | 0.5646 μs |  0.91 |    0.10 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 6.351 μs |  9.457 μs | 0.5184 μs |  1.14 |    0.11 |    1 |      64 B |        1.14 |
