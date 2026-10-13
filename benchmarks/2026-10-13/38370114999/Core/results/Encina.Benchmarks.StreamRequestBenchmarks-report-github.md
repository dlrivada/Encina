```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  11.922 μs |  1.8788 μs | 0.1030 μs |  1.00 |    0.01 |  0.7629 |      - |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.062 μs |  6.3006 μs | 0.3454 μs |  6.80 |    0.06 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 787.071 μs | 56.2785 μs | 3.0848 μs | 66.02 |    0.54 | 48.8281 |      - |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  64.194 μs |  2.6783 μs | 0.1468 μs |  5.38 |    0.04 |  3.0518 |      - |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  89.673 μs |  5.2175 μs | 0.2860 μs |  7.52 |    0.06 |  5.7373 |      - |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  80.367 μs |  5.8382 μs | 0.3200 μs |  6.74 |    0.06 |  5.1270 |      - |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 119.552 μs | 11.1588 μs | 0.6116 μs | 10.03 |    0.09 |  6.1035 | 0.1221 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.260 μs |  0.3106 μs | 0.0170 μs |  0.36 |    0.00 |  0.0229 |      - |     400 B |        0.03 |
