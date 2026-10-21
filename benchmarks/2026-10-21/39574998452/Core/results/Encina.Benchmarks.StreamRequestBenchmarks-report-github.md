```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.776 μs |  1.203 μs | 0.0660 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  85.155 μs |  5.745 μs | 0.3149 μs |  6.67 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 800.929 μs | 59.431 μs | 3.2576 μs | 62.69 |    0.36 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  68.980 μs |  6.747 μs | 0.3698 μs |  5.40 |    0.03 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  90.376 μs |  6.508 μs | 0.3567 μs |  7.07 |    0.04 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  86.250 μs |  7.685 μs | 0.4213 μs |  6.75 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 130.329 μs |  9.677 μs | 0.5304 μs | 10.20 |    0.06 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.002 μs |  1.152 μs | 0.0631 μs |  0.31 |    0.00 |  0.0229 |     400 B |        0.03 |
