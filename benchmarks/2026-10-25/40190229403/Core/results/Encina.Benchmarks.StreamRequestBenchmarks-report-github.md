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
| Stream_SmallDataset_10Items             |  12.474 μs |   0.9730 μs | 0.0533 μs |  1.00 |    0.01 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.290 μs |   2.6915 μs | 0.1475 μs |  6.52 |    0.03 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 771.053 μs | 131.5120 μs | 7.2086 μs | 61.81 |    0.55 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  66.989 μs |  10.1395 μs | 0.5558 μs |  5.37 |    0.04 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  85.855 μs |  13.0334 μs | 0.7144 μs |  6.88 |    0.06 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  82.296 μs |   8.6420 μs | 0.4737 μs |  6.60 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 129.148 μs |  49.3628 μs | 2.7057 μs | 10.35 |    0.19 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   3.968 μs |   0.2469 μs | 0.0135 μs |  0.32 |    0.00 |  0.0229 |     400 B |        0.03 |
