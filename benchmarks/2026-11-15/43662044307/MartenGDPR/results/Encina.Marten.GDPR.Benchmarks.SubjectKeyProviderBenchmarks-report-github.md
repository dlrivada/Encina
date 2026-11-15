```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **113.56 ns** |   **1.316 ns** |   **1.929 ns** |  **0.92** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,493.05 ns | 151.759 ns | 217.648 ns | 28.38 |    1.78 |    4 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    57.96 ns |   0.790 ns |   1.158 ns |  0.47 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   123.12 ns |   1.290 ns |   1.766 ns |  1.00 |    0.02 |    3 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |            |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **121.00 ns** |   **1.092 ns** |   **1.566 ns** |  **0.95** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,495.76 ns | 116.475 ns | 167.045 ns | 27.38 |    1.41 |    4 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    60.64 ns |   0.408 ns |   0.572 ns |  0.47 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   127.73 ns |   1.912 ns |   2.743 ns |  1.00 |    0.03 |    3 | 0.0196 |      - |     328 B |        1.00 |
