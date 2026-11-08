```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **228.0 ns** |  **1.21 ns** |  **1.77 ns** |   **228.6 ns** |  **0.95** |    **0.03** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 4,241.9 ns | 57.45 ns | 80.53 ns | 4,223.3 ns | 17.73 |    0.63 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   116.5 ns |  1.92 ns |  2.69 ns |   114.6 ns |  0.49 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   239.5 ns |  5.03 ns |  7.38 ns |   233.5 ns |  1.00 |    0.04 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |            |          |          |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **222.8 ns** |  **1.77 ns** |  **2.48 ns** |   **221.8 ns** |  **0.95** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 4,242.2 ns | 52.62 ns | 73.77 ns | 4,217.5 ns | 18.04 |    0.31 |    4 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   110.7 ns |  0.21 ns |  0.31 ns |   110.6 ns |  0.47 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   235.1 ns |  0.59 ns |  0.83 ns |   235.1 ns |  1.00 |    0.00 |    3 | 0.0196 |      - |     328 B |        1.00 |
