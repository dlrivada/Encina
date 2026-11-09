
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean       | Error       | StdDev      | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |-----------:|------------:|------------:|-----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2,587.3 ns** |  **1,172.9 ns** |    **64.29 ns** | **2,614.0 ns** |  **1.00** |    **0.03** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |   988.0 ns | 12,705.8 ns |   696.45 ns |   601.0 ns |  0.38 |    0.23 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1,175.3 ns | 17,232.7 ns |   944.58 ns |   702.0 ns |  0.45 |    0.32 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 2,270.0 ns | 21,456.5 ns | 1,176.10 ns | 1,943.0 ns |  0.88 |    0.39 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 4,125.7 ns | 15,488.9 ns |   849.00 ns | 3,855.0 ns |  1.60 |    0.29 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 1,639.3 ns | 20,718.5 ns | 1,135.65 ns | 1,092.0 ns |  0.63 |    0.38 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 1,449.0 ns | 22,452.2 ns | 1,230.68 ns | 1,032.0 ns |  0.56 |    0.41 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 2,466.5 ns | 33,900.6 ns | 1,858.20 ns | 1,531.5 ns |  0.95 |    0.62 |    3 |     152 B |        2.71 |
                                  |            |            |             |             |            |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **1,998.0 ns** | **16,966.8 ns** |   **930.01 ns** | **1,517.0 ns** |  **1.13** |    **0.60** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1,035.0 ns | 11,744.0 ns |   643.73 ns |   801.0 ns |  0.59 |    0.38 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 1,211.7 ns | 14,983.9 ns |   821.32 ns |   901.0 ns |  0.68 |    0.47 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 1,936.3 ns | 21,363.4 ns | 1,171.00 ns | 1,432.0 ns |  1.09 |    0.69 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 5,780.2 ns | 10,795.0 ns |   591.71 ns | 6,043.5 ns |  3.27 |    1.09 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 1,785.8 ns | 16,062.3 ns |   880.43 ns | 1,391.5 ns |  1.01 |    0.55 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 1,823.0 ns | 22,981.7 ns | 1,259.70 ns | 1,172.0 ns |  1.03 |    0.72 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 2,836.0 ns | 29,354.8 ns | 1,609.03 ns | 2,268.0 ns |  1.60 |    0.97 |    3 |     152 B |        2.71 |
