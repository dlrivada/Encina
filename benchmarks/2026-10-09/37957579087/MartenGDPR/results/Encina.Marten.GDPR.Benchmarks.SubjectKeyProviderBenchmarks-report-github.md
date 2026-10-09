```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **163.98 ns** |   **136.83 ns** |   **7.500 ns** |  **1.11** |    **0.05** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,730.05 ns | 2,720.26 ns | 149.107 ns | 18.48 |    0.97 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    73.49 ns |    68.21 ns |   3.739 ns |  0.50 |    0.02 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   147.81 ns |    72.07 ns |   3.950 ns |  1.00 |    0.03 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |             |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **173.07 ns** |   **121.14 ns** |   **6.640 ns** |  **1.03** |    **0.04** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 2,835.27 ns | 2,871.94 ns | 157.421 ns | 16.95 |    0.84 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    77.35 ns |   138.19 ns |   7.574 ns |  0.46 |    0.04 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   167.26 ns |    38.27 ns |   2.098 ns |  1.00 |    0.02 |    2 | 0.0038 |      - |     328 B |        1.00 |
