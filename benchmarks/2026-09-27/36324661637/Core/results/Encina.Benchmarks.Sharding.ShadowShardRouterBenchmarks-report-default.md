
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.755 μs** | **16.572 μs** | **0.9083 μs** |  **1.04** |    **0.30** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 4.645 μs |  6.620 μs | 0.3628 μs |  1.28 |    0.26 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 5.016 μs | 11.485 μs | 0.6295 μs |  1.38 |    0.30 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 4.506 μs | 31.781 μs | 1.7420 μs |  1.24 |    0.48 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 4.888 μs | 24.914 μs | 1.3656 μs |  1.35 |    0.42 |    2 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.218 μs** |  **9.415 μs** | **0.5161 μs** |  **1.02** |    **0.19** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 5.693 μs |  8.241 μs | 0.4517 μs |  1.80 |    0.26 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 7.014 μs | 13.153 μs | 0.7210 μs |  2.22 |    0.35 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 6.239 μs |  1.288 μs | 0.0706 μs |  1.97 |    0.26 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 7.025 μs | 12.235 μs | 0.6706 μs |  2.22 |    0.34 |    2 |      64 B |        1.14 |
