```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.300 μs |   0.2554 μs | 0.0140 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.121 μs |  14.4789 μs | 0.7936 μs |  6.60 |    0.06 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 789.220 μs | 116.9348 μs | 6.4096 μs | 64.17 |    0.46 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  68.021 μs |   3.8894 μs | 0.2132 μs |  5.53 |    0.02 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  86.569 μs |  19.8585 μs | 1.0885 μs |  7.04 |    0.08 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  83.527 μs |   8.4198 μs | 0.4615 μs |  6.79 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 130.405 μs |   6.7067 μs | 0.3676 μs | 10.60 |    0.03 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.010 μs |   0.4061 μs | 0.0223 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
