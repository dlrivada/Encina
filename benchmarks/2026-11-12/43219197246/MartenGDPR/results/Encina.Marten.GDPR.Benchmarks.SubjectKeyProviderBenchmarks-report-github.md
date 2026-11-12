```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **157.69 ns** |   **107.68 ns** |   **5.902 ns** |  **1.05** |    **0.04** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,434.42 ns | 4,079.99 ns | 223.638 ns | 16.20 |    1.31 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    79.60 ns |   100.44 ns |   5.505 ns |  0.53 |    0.03 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   150.29 ns |    48.79 ns |   2.675 ns |  1.00 |    0.02 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |             |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **164.21 ns** |    **93.85 ns** |   **5.144 ns** |  **1.06** |    **0.03** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 2,514.81 ns | 3,506.87 ns | 192.223 ns | 16.17 |    1.09 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    77.47 ns |    27.72 ns |   1.520 ns |  0.50 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   155.58 ns |    46.78 ns |   2.564 ns |  1.00 |    0.02 |    2 | 0.0038 |      - |     328 B |        1.00 |
