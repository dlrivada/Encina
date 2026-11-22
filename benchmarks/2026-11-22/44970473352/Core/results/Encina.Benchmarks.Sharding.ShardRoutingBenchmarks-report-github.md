```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                           | ShardCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **1,688.5 ns** | **426.73 ns** | **638.71 ns** | **1,958.0 ns** |  **1.20** |    **0.76** |    **5** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1,426.9 ns | 431.80 ns | 632.93 ns | 1,723.0 ns |  1.01 |    0.69 |    5 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          |   453.1 ns | 101.79 ns | 135.88 ns |   420.0 ns |  0.32 |    0.19 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          |   924.3 ns | 228.60 ns | 312.91 ns |   881.5 ns |  0.66 |    0.40 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 2,007.6 ns | 706.51 ns | 967.07 ns | 1,682.8 ns |  1.43 |    1.01 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2,318.3 ns | 143.51 ns | 201.18 ns | 2,283.0 ns |  1.65 |    0.81 |    6 |     104 B |        1.86 |
| GetShardConnectionString         | 3          |   564.7 ns |  94.92 ns | 133.06 ns |   531.0 ns |  0.40 |    0.22 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 1,111.6 ns | 214.77 ns | 301.08 ns | 1,017.0 ns |  0.79 |    0.45 |    4 |     152 B |        2.71 |
|                                  |            |            |           |           |            |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         |   **975.6 ns** | **104.48 ns** | **143.02 ns** |   **969.0 ns** |  **1.02** |    **0.21** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         |   648.8 ns |  47.58 ns |  66.69 ns |   655.5 ns |  0.68 |    0.12 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 1,261.6 ns | 466.77 ns | 684.19 ns |   872.0 ns |  1.32 |    0.73 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 1,867.7 ns | 706.96 ns | 991.06 ns | 1,232.0 ns |  1.95 |    1.06 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 2,864.8 ns | 188.50 ns | 270.35 ns | 2,874.0 ns |  3.00 |    0.51 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 1,800.4 ns | 542.12 ns | 777.49 ns | 1,266.8 ns |  1.88 |    0.85 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         |   932.0 ns | 143.50 ns | 201.16 ns |   916.0 ns |  0.97 |    0.25 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 2,525.1 ns | 581.00 ns | 775.62 ns | 3,005.0 ns |  2.64 |    0.88 |    4 |     152 B |        2.71 |
