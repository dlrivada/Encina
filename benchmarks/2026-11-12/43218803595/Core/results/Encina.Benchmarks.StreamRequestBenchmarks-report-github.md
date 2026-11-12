```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |   9.341 μs |   0.5913 μs | 0.0324 μs |  1.00 |    0.00 |  0.7629 |      - |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  64.329 μs |  15.5058 μs | 0.8499 μs |  6.89 |    0.08 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 607.778 μs | 104.4881 μs | 5.7273 μs | 65.07 |    0.57 | 48.8281 |      - |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  51.316 μs |  23.7912 μs | 1.3041 μs |  5.49 |    0.12 |  3.0518 |      - |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  68.326 μs |   7.6232 μs | 0.4179 μs |  7.31 |    0.04 |  5.7373 |      - |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  63.517 μs |   5.0331 μs | 0.2759 μs |  6.80 |    0.03 |  5.1270 |      - |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       |  94.883 μs |  11.8591 μs | 0.6500 μs | 10.16 |    0.07 |  6.1035 | 0.1221 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.427 μs |   1.1753 μs | 0.0644 μs |  0.37 |    0.01 |  0.0229 |      - |     400 B |        0.03 |
