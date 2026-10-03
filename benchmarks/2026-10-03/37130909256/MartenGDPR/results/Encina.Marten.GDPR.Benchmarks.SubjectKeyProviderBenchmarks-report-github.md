```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |    **85.40 ns** |    **20.281 ns** |  **1.112 ns** |  **1.14** |    **0.01** |    **2** | **0.0167** |      **-** |     **280 B** |        **1.03** |
| CreateNewKey           | 10           | 3,159.89 ns | 1,119.430 ns | 61.360 ns | 42.33 |    0.75 |    3 | 0.0534 | 0.0496 |     952 B |        3.50 |
| CheckIsForgotten       | 10           |    59.44 ns |     5.497 ns |  0.301 ns |  0.80 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |    74.65 ns |     8.489 ns |  0.465 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |              |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |    **91.36 ns** |    **39.180 ns** |  **2.148 ns** |  **1.27** |    **0.03** |    **3** | **0.0167** |      **-** |     **280 B** |        **1.03** |
| CreateNewKey           | 100          | 3,189.37 ns |   535.092 ns | 29.330 ns | 44.40 |    0.46 |    4 | 0.0534 | 0.0496 |     952 B |        3.50 |
| CheckIsForgotten       | 100          |    59.25 ns |     2.018 ns |  0.111 ns |  0.82 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |    71.84 ns |    10.049 ns |  0.551 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
