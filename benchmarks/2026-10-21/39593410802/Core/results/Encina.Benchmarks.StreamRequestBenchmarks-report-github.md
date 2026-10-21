```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 3.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   8.124 μs |   1.1602 μs |  0.0636 μs |  1.00 |    0.01 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  53.030 μs |   2.1663 μs |  0.1187 μs |  6.53 |    0.05 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 495.072 μs | 314.9911 μs | 17.2657 μs | 60.94 |    1.89 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  46.861 μs |   2.4609 μs |  0.1349 μs |  5.77 |    0.04 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  57.046 μs |   4.6776 μs |  0.2564 μs |  7.02 |    0.06 |  5.7373 | 0.0610 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  53.570 μs |   2.3784 μs |  0.1304 μs |  6.59 |    0.05 |  5.1880 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  79.714 μs |  10.1477 μs |  0.5562 μs |  9.81 |    0.09 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   2.833 μs |   0.7971 μs |  0.0437 μs |  0.35 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
