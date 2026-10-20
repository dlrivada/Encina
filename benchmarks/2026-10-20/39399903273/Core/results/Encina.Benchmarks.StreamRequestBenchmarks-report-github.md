```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------------:|-----------:|------:|--------:|--------:|----------:|------------:|
| Stream_SmallDataset_10Items             |  12.183 μs |   0.6576 μs |  0.0360 μs |  1.00 |    0.00 |  0.7629 |   12864 B |        1.00 |
| Stream_MediumDataset_100Items           |  81.411 μs |  15.4352 μs |  0.8461 μs |  6.68 |    0.06 |  5.1270 |   87025 B |        6.77 |
| Stream_LargeDataset_1000Items           | 776.560 μs | 214.7979 μs | 11.7738 μs | 63.74 |    0.85 | 48.8281 |  828636 B |       64.42 |
| Stream_WithPipelineBehaviors            |  67.803 μs |   2.6582 μs |  0.1457 μs |  5.57 |    0.02 |  3.0518 |   51553 B |        4.01 |
| Stream_MaterializeToList_100Items       |  90.262 μs |   9.0180 μs |  0.4943 μs |  7.41 |    0.04 |  5.7373 |   96841 B |        7.53 |
| Stream_CountOnly_NoMaterialization      |  84.171 μs |  10.7277 μs |  0.5880 μs |  6.91 |    0.05 |  5.1270 |   87025 B |        6.77 |
| Stream_WithCancellation_EarlyExit       | 127.523 μs |  17.8470 μs |  0.9783 μs | 10.47 |    0.07 |  6.1035 |  102801 B |        7.99 |
| Stream_DirectHandlerInvocation_NoEncina |   4.006 μs |   0.6360 μs |  0.0349 μs |  0.33 |    0.00 |  0.0229 |     400 B |        0.03 |
