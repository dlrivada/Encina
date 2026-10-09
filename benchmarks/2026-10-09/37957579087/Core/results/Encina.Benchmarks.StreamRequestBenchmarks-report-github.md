```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.730 μs |  2.5904 μs | 0.1420 μs |  1.00 |    0.01 |  0.7629 |   12856 B |        1.00 |
| Stream_MediumDataset_100Items           |  84.085 μs | 15.8146 μs | 0.8669 μs |  6.61 |    0.09 |  5.1270 |   87017 B |        6.77 |
| Stream_LargeDataset_1000Items           | 786.434 μs | 25.0584 μs | 1.3735 μs | 61.78 |    0.61 | 48.8281 |  828628 B |       64.45 |
| Stream_WithPipelineBehaviors            |  68.524 μs |  3.5972 μs | 0.1972 μs |  5.38 |    0.05 |  3.0518 |   51545 B |        4.01 |
| Stream_MaterializeToList_100Items       |  89.426 μs |  1.5869 μs | 0.0870 μs |  7.03 |    0.07 |  5.7373 |   96833 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.138 μs |  5.0096 μs | 0.2746 μs |  6.45 |    0.07 |  5.1270 |   87017 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 126.882 μs |  4.8372 μs | 0.2651 μs |  9.97 |    0.10 |  6.1035 |  102793 B |        8.00 |
| Stream_DirectHandlerInvocation_NoEncina |   4.100 μs |  0.2561 μs | 0.0140 μs |  0.32 |    0.00 |  0.0229 |     400 B |        0.03 |
