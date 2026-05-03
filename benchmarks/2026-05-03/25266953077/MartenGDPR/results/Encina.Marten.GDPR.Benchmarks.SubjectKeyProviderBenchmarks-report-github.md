```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|---------:|----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **158.4 ns** |  **2.02 ns** |   **2.90 ns** |   **158.7 ns** |  **1.23** |    **0.02** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,865.5 ns | 85.59 ns | 117.15 ns | 4,851.3 ns | 37.84 |    0.94 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   110.6 ns |  1.11 ns |   1.59 ns |   110.3 ns |  0.86 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   128.6 ns |  0.68 ns |   1.01 ns |   128.6 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |          |           |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **150.8 ns** |  **1.55 ns** |   **2.27 ns** |   **152.5 ns** |  **1.14** |    **0.02** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,886.4 ns | 83.97 ns | 117.72 ns | 4,878.7 ns | 37.02 |    1.04 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   106.0 ns |  0.09 ns |   0.13 ns |   106.0 ns |  0.80 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   132.0 ns |  1.43 ns |   2.05 ns |   131.7 ns |  1.00 |    0.02 |    2 | 0.0162 |      - |     272 B |        1.00 |
