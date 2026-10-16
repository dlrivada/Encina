```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **131.59 ns** |   **160.221 ns** |   **8.782 ns** |  **1.03** |    **0.06** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,606.05 ns | 2,354.051 ns | 129.033 ns | 28.13 |    0.87 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    67.09 ns |    15.163 ns |   0.831 ns |  0.52 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   128.20 ns |     3.805 ns |   0.209 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **132.65 ns** |    **96.454 ns** |   **5.287 ns** |  **1.04** |    **0.04** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,378.40 ns | 1,918.369 ns | 105.152 ns | 26.52 |    0.76 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    58.18 ns |     9.460 ns |   0.519 ns |  0.46 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   127.39 ns |    25.908 ns |   1.420 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
