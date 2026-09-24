```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **122.51 ns** |     **5.119 ns** |  **0.281 ns** |  **1.21** |    **0.01** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,805.13 ns | 1,174.883 ns | 64.399 ns | 37.44 |    0.58 |    4 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |    82.42 ns |     0.307 ns |  0.017 ns |  0.81 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   101.62 ns |    11.005 ns |  0.603 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |              |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **127.72 ns** |    **12.216 ns** |  **0.670 ns** |  **1.29** |    **0.02** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,795.77 ns |   436.291 ns | 23.915 ns | 38.48 |    0.61 |    3 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |    87.14 ns |     3.835 ns |  0.210 ns |  0.88 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |    98.67 ns |    31.271 ns |  1.714 ns |  1.00 |    0.02 |    1 | 0.0162 |      - |     272 B |        1.00 |
