```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **123.82 ns** |  **15.795 ns** |  **0.866 ns** |  **1.23** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,765.65 ns | 261.156 ns | 14.315 ns | 37.32 |    0.14 |    3 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |    86.90 ns |   5.773 ns |  0.316 ns |  0.86 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   100.91 ns |   4.261 ns |  0.234 ns |  1.00 |    0.00 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |            |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **127.15 ns** |  **29.939 ns** |  **1.641 ns** |  **1.25** |    **0.01** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,836.77 ns | 963.188 ns | 52.796 ns | 37.79 |    0.48 |    4 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |    82.85 ns |   3.502 ns |  0.192 ns |  0.82 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   101.53 ns |   9.221 ns |  0.505 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
