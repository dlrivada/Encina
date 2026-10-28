```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   6.711 μs |  1.5918 μs | 0.0873 μs |  1.00 |    0.02 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  42.508 μs |  4.3493 μs | 0.2384 μs |  6.33 |    0.08 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 394.404 μs | 26.3326 μs | 1.4434 μs | 58.78 |    0.69 | 49.3164 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  38.803 μs | 26.2171 μs | 1.4370 μs |  5.78 |    0.20 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  43.901 μs |  7.3887 μs | 0.4050 μs |  6.54 |    0.09 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  42.618 μs | 12.1972 μs | 0.6686 μs |  6.35 |    0.11 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  65.092 μs | 14.4399 μs | 0.7915 μs |  9.70 |    0.15 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.296 μs |  0.1484 μs | 0.0081 μs |  0.34 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
