```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **118.38 ns** |  **13.998 ns** |  **0.767 ns** |  **0.93** |    **0.06** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,995.08 ns | 647.662 ns | 35.501 ns | 23.55 |    1.49 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    58.62 ns |  91.216 ns |  5.000 ns |  0.46 |    0.04 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   127.65 ns | 175.592 ns |  9.625 ns |  1.00 |    0.09 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |            |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **124.98 ns** |  **80.687 ns** |  **4.423 ns** |  **1.10** |    **0.04** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,035.61 ns | 884.478 ns | 48.481 ns | 26.63 |    0.50 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    57.27 ns |   5.535 ns |  0.303 ns |  0.50 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   114.02 ns |  29.852 ns |  1.636 ns |  1.00 |    0.02 |    2 | 0.0196 |      - |     328 B |        1.00 |
