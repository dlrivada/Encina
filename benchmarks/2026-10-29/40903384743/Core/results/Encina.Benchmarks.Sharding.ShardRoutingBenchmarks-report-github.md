```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean       | Error       | StdDev     | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |-----------:|------------:|-----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2,771.3 ns** |  **5,905.7 ns** |   **323.7 ns** | **2,855.0 ns** |  **1.01** |    **0.15** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |   934.7 ns | 15,408.7 ns |   844.6 ns |   551.0 ns |  0.34 |    0.27 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2,300.2 ns | 22,605.9 ns | 1,239.1 ns | 2,383.5 ns |  0.84 |    0.40 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 2,302.8 ns | 33,486.3 ns | 1,835.5 ns | 1,381.5 ns |  0.84 |    0.59 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4,250.8 ns | 30,819.3 ns | 1,689.3 ns | 4,160.5 ns |  1.55 |    0.56 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2,050.0 ns | 29,766.1 ns | 1,631.6 ns | 1,453.0 ns |  0.75 |    0.52 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2,040.8 ns | 29,949.2 ns | 1,641.6 ns | 1,376.5 ns |  0.74 |    0.53 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 3,233.3 ns | 48,648.0 ns | 2,666.6 ns | 2,189.0 ns |  1.18 |    0.85 |    4 |     152 B |        2.71 |
|                                  |            |            |             |            |            |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3,112.3 ns** | **32,675.6 ns** | **1,791.1 ns** | **2,488.0 ns** |  **1.22** |    **0.84** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1,661.3 ns | 15,108.9 ns |   828.2 ns | 1,498.0 ns |  0.65 |    0.41 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3,273.3 ns |  3,695.6 ns |   202.6 ns | 3,210.0 ns |  1.29 |    0.55 |    3 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3,707.2 ns | 34,739.3 ns | 1,904.2 ns | 3,089.5 ns |  1.46 |    0.94 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7,334.7 ns | 59,721.1 ns | 3,273.5 ns | 5,769.0 ns |  2.88 |    1.72 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3,388.7 ns | 34,605.2 ns | 1,896.8 ns | 2,804.0 ns |  1.33 |    0.90 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3,510.0 ns | 43,507.7 ns | 2,384.8 ns | 2,428.0 ns |  1.38 |    1.06 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 3,809.3 ns | 36,358.7 ns | 1,992.9 ns | 2,704.0 ns |  1.50 |    0.97 |    3 |     152 B |        2.71 |
