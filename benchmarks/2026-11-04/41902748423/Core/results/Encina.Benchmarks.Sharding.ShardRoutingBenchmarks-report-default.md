
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean        | Error        | StdDev      | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |------------:|-------------:|------------:|-----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **2,455.3 ns** |   **6,116.7 ns** |    **335.3 ns** | **2,569.0 ns** |  **1.01** |    **0.18** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |    985.0 ns |  14,682.2 ns |    804.8 ns |   651.0 ns |  0.41 |    0.29 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  1,637.3 ns |   9,159.0 ns |    502.0 ns | 1,838.0 ns |  0.68 |    0.20 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  3,319.5 ns |  15,789.8 ns |    865.5 ns | 2,928.5 ns |  1.37 |    0.36 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  2,807.3 ns |  23,725.1 ns |  1,300.5 ns | 2,344.0 ns |  1.16 |    0.49 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3,061.3 ns |  17,942.0 ns |    983.5 ns | 2,534.0 ns |  1.26 |    0.39 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  1,652.5 ns |  19,154.5 ns |  1,049.9 ns | 1,071.5 ns |  0.68 |    0.39 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  2,887.7 ns |  33,063.5 ns |  1,812.3 ns | 2,123.0 ns |  1.19 |    0.67 |    3 |     152 B |        2.71 |
                                  |            |             |              |             |            |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **1,910.0 ns** |  **16,489.9 ns** |    **903.9 ns** | **1,613.0 ns** |  **1.15** |    **0.64** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2,167.5 ns |   8,577.9 ns |    470.2 ns | 2,398.5 ns |  1.30 |    0.53 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2,543.5 ns |   4,753.9 ns |    260.6 ns | 2,563.5 ns |  1.53 |    0.57 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  2,070.0 ns |  19,043.7 ns |  1,043.8 ns | 1,492.0 ns |  1.24 |    0.73 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         |  4,238.3 ns |  19,691.0 ns |  1,079.3 ns | 3,650.0 ns |  2.54 |    1.09 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 19,827.5 ns | 592,852.0 ns | 32,496.2 ns | 1,216.5 ns | 11.90 |   18.34 |    4 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  3,505.3 ns |  15,248.2 ns |    835.8 ns | 3,095.0 ns |  2.10 |    0.88 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  3,094.0 ns |  30,434.2 ns |  1,668.2 ns | 2,333.0 ns |  1.86 |    1.13 |    1 |     152 B |        2.71 |
