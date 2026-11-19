
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean       | Error       | StdDev     | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |-----------:|------------:|-----------:|-----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2,841.3 ns** |  **6,457.6 ns** |   **354.0 ns** | **2,695.0 ns** |  **1.01** |    **0.15** |    **5** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |   951.7 ns | 13,983.1 ns |   766.5 ns |   581.0 ns |  0.34 |    0.24 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1,179.5 ns | 16,533.6 ns |   906.3 ns |   775.5 ns |  0.42 |    0.28 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 1,799.7 ns | 22,487.0 ns | 1,232.6 ns | 1,222.0 ns |  0.64 |    0.39 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 2,465.7 ns | 25,810.3 ns | 1,414.7 ns | 1,968.0 ns |  0.88 |    0.45 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 1,561.8 ns | 20,673.9 ns | 1,133.2 ns | 1,231.5 ns |  0.56 |    0.35 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 1,353.7 ns | 21,106.1 ns | 1,156.9 ns |   836.0 ns |  0.48 |    0.36 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 2,831.3 ns | 32,243.7 ns | 1,767.4 ns | 2,094.0 ns |  1.01 |    0.56 |    4 |     152 B |        2.71 |
                                  |            |            |             |            |            |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3,762.3 ns** |  **8,672.4 ns** |   **475.4 ns** | **3,556.0 ns** |  **1.01** |    **0.15** |    **6** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1,035.0 ns | 12,648.8 ns |   693.3 ns |   702.0 ns |  0.28 |    0.16 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 1,312.0 ns | 16,891.5 ns |   925.9 ns |   951.0 ns |  0.35 |    0.22 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 2,677.3 ns | 19,408.4 ns | 1,063.8 ns | 2,263.0 ns |  0.72 |    0.26 |    5 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 3,876.8 ns | 20,569.9 ns | 1,127.5 ns | 3,339.5 ns |  1.04 |    0.28 |    6 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 1,708.8 ns | 17,601.6 ns |   964.8 ns | 1,281.5 ns |  0.46 |    0.23 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 2,302.0 ns | 25,683.3 ns | 1,407.8 ns | 1,698.0 ns |  0.62 |    0.33 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 2,668.5 ns | 31,188.8 ns | 1,709.6 ns | 1,867.5 ns |  0.72 |    0.41 |    4 |     152 B |        2.71 |
