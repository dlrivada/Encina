```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.560 μs |  0.8281 μs | 0.0454 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  83.215 μs |  7.7832 μs | 0.4266 μs |  6.63 |    0.04 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 795.655 μs | 14.1950 μs | 0.7781 μs | 63.35 |    0.21 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  68.411 μs |  8.4019 μs | 0.4605 μs |  5.45 |    0.04 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  89.529 μs |  1.8699 μs | 0.1025 μs |  7.13 |    0.02 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  84.809 μs |  1.1150 μs | 0.0611 μs |  6.75 |    0.02 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 136.376 μs |  6.5272 μs | 0.3578 μs | 10.86 |    0.04 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.088 μs |  0.5176 μs | 0.0284 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
