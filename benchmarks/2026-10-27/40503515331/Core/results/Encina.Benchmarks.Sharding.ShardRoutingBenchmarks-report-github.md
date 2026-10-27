```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean       | Error       | StdDev     | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |-----------:|------------:|-----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **1,603.5 ns** | **20,499.6 ns** | **1,123.7 ns** | **1,116.5 ns** |  **1.33** |    **1.08** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |   996.7 ns | 13,393.7 ns |   734.2 ns |   666.0 ns |  0.83 |    0.70 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 1,076.5 ns | 16,731.1 ns |   917.1 ns |   655.5 ns |  0.89 |    0.83 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 1,831.3 ns | 24,865.3 ns | 1,363.0 ns | 1,157.0 ns |  1.52 |    1.29 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 2,645.7 ns | 26,241.2 ns | 1,438.4 ns | 2,129.0 ns |  2.19 |    1.53 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 1,498.5 ns | 24,601.0 ns | 1,348.5 ns |   770.5 ns |  1.24 |    1.21 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2,868.0 ns | 15,716.2 ns |   861.5 ns | 2,474.0 ns |  2.37 |    1.31 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4,219.3 ns | 22,712.8 ns | 1,245.0 ns | 3,976.0 ns |  3.49 |    1.92 |    4 |     152 B |        2.71 |
|                                  |            |            |             |            |            |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **2,013.3 ns** | **15,835.6 ns** |   **868.0 ns** | **1,543.0 ns** |  **1.11** |    **0.54** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1,142.0 ns | 12,256.9 ns |   671.8 ns |   892.0 ns |  0.63 |    0.38 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2,028.3 ns | 17,302.7 ns |   948.4 ns | 2,098.0 ns |  1.12 |    0.58 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 2,719.0 ns | 26,057.3 ns | 1,428.3 ns | 2,109.0 ns |  1.50 |    0.84 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5,078.0 ns | 33,877.4 ns | 1,856.9 ns | 4,277.0 ns |  2.80 |    1.25 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 1,888.0 ns | 19,584.3 ns | 1,073.5 ns | 1,457.0 ns |  1.04 |    0.62 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 2,115.0 ns | 21,854.4 ns | 1,197.9 ns | 1,558.0 ns |  1.17 |    0.69 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 2,989.7 ns | 34,065.8 ns | 1,867.3 ns | 2,008.0 ns |  1.65 |    1.05 |    3 |     152 B |        2.71 |
