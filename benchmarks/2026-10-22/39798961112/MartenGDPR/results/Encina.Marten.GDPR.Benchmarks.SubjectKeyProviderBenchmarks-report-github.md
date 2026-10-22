```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|---------:|----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **236.7 ns** |  **0.90 ns** |   **1.29 ns** |   **236.8 ns** |  **0.96** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 4,580.1 ns | 48.61 ns |  64.90 ns | 4,584.9 ns | 18.64 |    0.41 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   117.7 ns |  0.15 ns |   0.21 ns |   117.7 ns |  0.48 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   245.8 ns |  3.08 ns |   4.21 ns |   249.0 ns |  1.00 |    0.02 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |            |          |           |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **232.8 ns** |  **3.33 ns** |   **4.89 ns** |   **233.4 ns** |  **0.96** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 4,763.8 ns | 96.06 ns | 134.67 ns | 4,758.0 ns | 19.70 |    0.58 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   113.9 ns |  0.62 ns |   0.90 ns |   113.3 ns |  0.47 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   241.8 ns |  1.70 ns |   2.44 ns |   242.0 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
