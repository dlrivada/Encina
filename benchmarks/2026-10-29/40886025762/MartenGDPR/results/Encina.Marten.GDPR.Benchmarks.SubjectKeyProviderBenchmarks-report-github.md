```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **123.00 ns** |    **91.547 ns** |   **5.018 ns** |  **1.01** |    **0.04** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,328.07 ns | 2,078.536 ns | 113.932 ns | 27.43 |    0.84 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    59.67 ns |    66.915 ns |   3.668 ns |  0.49 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   121.33 ns |    20.588 ns |   1.129 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **119.47 ns** |    **98.978 ns** |   **5.425 ns** |  **0.93** |    **0.06** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,280.20 ns | 1,372.713 ns |  75.243 ns | 25.45 |    1.48 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    59.20 ns |     3.388 ns |   0.186 ns |  0.46 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   129.26 ns |   154.016 ns |   8.442 ns |  1.00 |    0.08 |    2 | 0.0196 |      - |     328 B |        1.00 |
